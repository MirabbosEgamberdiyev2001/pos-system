using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Models;
using POS.Application.Common.Exceptions;
using POS.Application.Mappings;
using POS.Application.Services;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;
using System.Linq.Expressions;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IUnitOfWork>    _unitOfWorkMock;
    private Mock<IProductInterface> _productRepoMock;
    private IMapper      _mapper;
    private IMemoryCache _cache;
    private ProductService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock  = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IProductInterface>();
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        // AnyAsync — default: false (barcode unique)
        _productRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _cache  = new MemoryCache(new MemoryCacheOptions());

        _service = new ProductService(
            _unitOfWorkMock.Object,
            NullLogger<ProductService>.Instance,
            _mapper,
            _cache);
    }

    [TearDown]
    public void TearDown() => _cache.Dispose();

    private static AddProductDto ValidDto() => new()
    {
        Name           = "Test Mahsulot",
        CategoryId     = 1,
        MeasurmentType = MeasurmentType.Dona,
        WarningAmount  = 5
    };

    // ─── AddAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task AddAsync_ValidDto_ReturnsCreatedProduct()
    {
        var dto    = ValidDto();
        var entity = new Product { Id = 1, Name = dto.Name, CategoryId = dto.CategoryId,
                                   LastModifiedDate = DateTime.UtcNow };

        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product>());
        _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).ReturnsAsync(entity);

        var result = await _service.AddAsync(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
    }

    [Test]
    public async Task AddAsync_EmptyName_ThrowsMarketException()
    {
        var dto = ValidDto();
        dto.Name = "";
        Func<Task> act = () => _service.AddAsync(dto);
        await act.Should().ThrowAsync<MarketException>();
    }

    [Test]
    public async Task AddAsync_DuplicateProduct_ThrowsMarketException()
    {
        var dto = ValidDto();
        var existing = new Product
        {
            Id = 1, Name = dto.Name, CategoryId = dto.CategoryId,
            Description = dto.Description, IsDeleted = false, LastModifiedDate = DateTime.UtcNow
        };
        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product> { existing });

        Func<Task> act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<MarketException>().WithMessage("*omborda mavjud*");
    }

    // ─── GenerateBarcodeAsync (O(1) — AnyAsync ishlatadi) ─────────────────

    [Test]
    public async Task GenerateBarcodeAsync_ReturnsNumericBarcode13Digits()
    {
        var barcode = await _service.GenerateBarcodeAsync();

        barcode.Should().NotBeNullOrEmpty();
        barcode.Length.Should().Be(13);
        long.TryParse(barcode, out _).Should().BeTrue();
    }

    [Test]
    public async Task GenerateBarcodeAsync_RetriesIfConflict()
    {
        // Birinchi urinish conflict — ikkinchisi yo'q
        _productRepoMock
            .SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true)   // 1-barcode conflict
            .ReturnsAsync(false); // 2-barcode unique

        var barcode = await _service.GenerateBarcodeAsync();

        barcode.Should().NotBeNullOrEmpty();
        _productRepoMock.Verify(
            r => r.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>()), Times.Exactly(2));
    }

    // ─── GetAll* ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllActivesAsync_ReturnsOnlyActiveProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Active",  IsDeleted = false, CategoryId = 1, LastModifiedDate = DateTime.UtcNow },
            new() { Id = 2, Name = "Deleted", IsDeleted = true,  CategoryId = 1, LastModifiedDate = DateTime.UtcNow }
        };
        _productRepoMock.Setup(r => r.GetAllWithCategories()).ReturnsAsync(products);

        var result = await _service.GetAllActivesAsync(0);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active");
    }

    [Test]
    public async Task GetAllArchivesAsync_ReturnsOnlyDeletedProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Active",  IsDeleted = false, CategoryId = 1, LastModifiedDate = DateTime.UtcNow },
            new() { Id = 2, Name = "Deleted", IsDeleted = true,  CategoryId = 1, LastModifiedDate = DateTime.UtcNow }
        };
        _productRepoMock.Setup(r => r.GetAllWithCategories()).ReturnsAsync(products);

        var result = await _service.GetAllArchivesAsync(0);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Deleted");
    }

    // ─── ActionAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task ActionAsync_Archive_SetsProductDeleted()
    {
        var product = new Product { Id = 1, Name = "Test", IsDeleted = false };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(product);

        await _service.ActionAsync(1, ActionType.Archive);

        _productRepoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.IsDeleted)), Times.Once);
    }

    // ─── Cache ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_SecondCall_UsesCacheNotDb()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "P1", IsDeleted = false, CategoryId = 1, LastModifiedDate = DateTime.UtcNow }
        };
        _productRepoMock.Setup(r => r.GetAllWithCategories()).ReturnsAsync(products);

        await _service.GetAllAsync();
        await _service.GetAllAsync();

        _productRepoMock.Verify(r => r.GetAllWithCategories(), Times.Once);
    }
}
