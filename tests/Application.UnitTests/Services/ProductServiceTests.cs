using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Exceptions;
using POS.Application.Common.Models;
using POS.Application.Services;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IProductInterface> _productRepoMock;
    private ProductService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IProductInterface>();
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _service = new ProductService(_unitOfWorkMock.Object, NullLogger<ProductService>.Instance);
    }

    private static AddProductDto ValidDto() => new()
    {
        Name = "Test Mahsulot",
        CategoryId = 1,
        MeasurmentType = MeasurmentType.Dona,
        WarningAmount = 5
    };

    [Test]
    public async Task AddAsync_ValidDto_ReturnsCreatedProduct()
    {
        var dto = ValidDto();
        var entity = new Product { Id = 1, Name = dto.Name, CategoryId = dto.CategoryId };

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
            Id = 1,
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            Description = dto.Description,
            IsDeleted = false
        };

        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product> { existing });

        Func<Task> act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<MarketException>()
            .WithMessage("*omborda mavjud*");
    }

    [Test]
    public async Task GenerateBarcodeAsync_ReturnsUniqueBarcode()
    {
        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product>());

        var barcode = await _service.GenerateBarcodeAsync();

        barcode.Should().NotBeNullOrEmpty();
        barcode.Length.Should().BeGreaterThanOrEqualTo(13);
        long.TryParse(barcode, out _).Should().BeTrue();
    }

    [Test]
    public async Task GenerateBarcodeAsync_SkipsExistingBarcodes()
    {
        var existing = new List<Product>
        {
            new() { Id = 1, Barcode = "1234567890123" }
        };
        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

        var barcode = await _service.GenerateBarcodeAsync();

        barcode.Should().NotBe("1234567890123");
    }

    [Test]
    public async Task GetAllActivesAsync_ReturnsOnlyActiveProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Active", IsDeleted = false, CategoryId = 1 },
            new() { Id = 2, Name = "Deleted", IsDeleted = true, CategoryId = 1 }
        };
        _productRepoMock.Setup(r => r.GetAllWithCategories()).ReturnsAsync(products);

        var result = await _service.GetAllActivesAsync(0);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active");
    }

    [Test]
    public async Task ActionAsync_Archive_SetsProductDeleted()
    {
        var product = new Product { Id = 1, Name = "Test", IsDeleted = false };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(product);

        await _service.ActionAsync(1, ActionType.Archive);

        _productRepoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.IsDeleted)), Times.Once);
    }
}
