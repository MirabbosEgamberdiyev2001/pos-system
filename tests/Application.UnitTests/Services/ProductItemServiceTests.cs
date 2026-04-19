using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using POS.Application.Common.DataTransferObjects.WarehouseItemDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Exceptions;
using POS.Application.Common.Models;
using POS.Application.Services;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class ProductItemServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IProductItemInterface> _itemRepoMock;
    private Mock<IProductInterface> _productRepoMock;
    private ProductItemService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _itemRepoMock = new Mock<IProductItemInterface>();
        _productRepoMock = new Mock<IProductInterface>();
        _unitOfWorkMock.Setup(u => u.ProductItems).Returns(_itemRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _service = new ProductItemService(_unitOfWorkMock.Object, NullLogger<ProductItemService>.Instance);
    }

    [Test]
    public async Task AddAsync_ValidDto_UpdatesProductAmount()
    {
        var dto = new AddProductItemDto
        {
            ProductId = 1,
            AdminId = 1,
            Amount = 50,
            BuyingPrice = 10_000,
            SellingPrice = 15_000
        };
        var product = new Product { Id = 1, Name = "Test", Amount = 0 };
        var savedItem = new ProductItem { Id = 1, ProductId = 1, Amount = 50, Product = product };

        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _itemRepoMock.Setup(r => r.AddAsync(It.IsAny<ProductItem>())).ReturnsAsync(savedItem);
        _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ProductItem> { savedItem });
        _itemRepoMock.Setup(r => r.GetByIdWithProductAsync(1)).ReturnsAsync(savedItem);
        _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(product);

        var result = await _service.AddAsync(dto);

        result.Should().NotBeNull();
        result.Amount.Should().Be(50);
        _productRepoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.Amount == 50)), Times.Once);
    }

    [Test]
    public async Task AddAsync_NegativePrice_ThrowsMarketException()
    {
        var dto = new AddProductItemDto { ProductId = 1, AdminId = 1, Amount = 10, BuyingPrice = -1, SellingPrice = 15 };

        Func<Task> act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<MarketException>();
    }

    [Test]
    public async Task AddAsync_SellingPriceLessThanBuying_ThrowsMarketException()
    {
        var dto = new AddProductItemDto { ProductId = 1, AdminId = 1, Amount = 10, BuyingPrice = 20_000, SellingPrice = 10_000 };

        Func<Task> act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<MarketException>()
            .WithMessage("*Sotuv narxi*");
    }

    [Test]
    public async Task AddAsync_ProductNotFound_ThrowsMarketException()
    {
        var dto = new AddProductItemDto { ProductId = 99, AdminId = 1, Amount = 10, BuyingPrice = 10, SellingPrice = 15 };
        _productRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        Func<Task> act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<MarketException>()
            .WithMessage("*topilmadi*");
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        var product = new Product { Id = 1, Name = "Test" };
        var items = new List<ProductItem>
        {
            new() { Id = 1, ProductId = 1, Amount = 10, Product = product },
            new() { Id = 2, ProductId = 1, Amount = 20, Product = product }
        };
        _itemRepoMock.Setup(r => r.GetAllWithProductAsync()).ReturnsAsync(items);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetPagedAsync_ReturnsPaginatedResult()
    {
        var product = new Product { Id = 1, Name = "P" };
        var items = Enumerable.Range(1, 15)
            .Select(i => new ProductItem { Id = i, ProductId = 1, Amount = i, IsDeleted = false, Product = product })
            .ToList();
        _itemRepoMock.Setup(r => r.GetAllWithProductAsync()).ReturnsAsync(items);

        var result = await _service.GetPagedAsync(pageSize: 5, pageNumber: 2, productId: 0);

        result.Data.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.CurrentPage.Should().Be(2);
    }

    [Test]
    public async Task ActionAsync_Archive_SetsIsDeleted()
    {
        var item = new ProductItem { Id = 1, ProductId = 1, IsDeleted = false, Amount = 10 };
        var product = new Product { Id = 1, Amount = 10 };

        _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _itemRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ProductItem>())).ReturnsAsync(item);
        _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ProductItem>());
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(product);

        await _service.ActionAsync(1, ActionType.Archive);

        _itemRepoMock.Verify(r => r.UpdateAsync(It.Is<ProductItem>(p => p.IsDeleted)), Times.Once);
    }
}
