using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using POS.Application.Common.DataTransferObjects.ReceiptDtos;
using POS.Application.Common.DataTransferObjects.TransactionDtos;
using POS.Application.Services;
using POS.Domain.Entities;
using POS.Domain.Entities.Selling;
using POS.Domain.Interfaces;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class ReceiptServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IReceiptInterface> _receiptRepoMock;
    private Mock<ITransactionInterface> _transactionRepoMock;
    private Mock<IProductInterface> _productRepoMock;
    private ReceiptService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _receiptRepoMock = new Mock<IReceiptInterface>();
        _transactionRepoMock = new Mock<ITransactionInterface>();
        _productRepoMock = new Mock<IProductInterface>();

        _unitOfWorkMock.Setup(u => u.Receipts).Returns(_receiptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _service = new ReceiptService(_unitOfWorkMock.Object, NullLogger<ReceiptService>.Instance);
    }

    [Test]
    public async Task AddAsync_ValidData_CreatesReceiptAndTransactions()
    {
        var dto = new AddReceiptDto { SellerId = 1, PaidCash = 50_000, PaidCard = 0 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "Olma", ProductPrice = 5_000, Quantity = 5, TotalPrice = 25_000 },
            new() { ProductId = 2, ProductName = "Non", ProductPrice = 2_500, Quantity = 4, TotalPrice = 10_000 }
        };
        var savedReceipt = new Receipt { Id = 10, SellerId = 1, TotalPrice = 35_000 };
        var product1 = new Product { Id = 1, Amount = 100 };
        var product2 = new Product { Id = 2, Amount = 50 };

        _receiptRepoMock.Setup(r => r.AddAsync(It.IsAny<Receipt>())).ReturnsAsync(savedReceipt);
        _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product1);
        _productRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product2);
        _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        var result = await _service.AddAsync(dto, items);

        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
    }

    [Test]
    public async Task AddAsync_InsufficientPayment_ThrowsInvalidOperationException()
    {
        var dto = new AddReceiptDto { SellerId = 1, PaidCash = 1_000, PaidCard = 0 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "Mahsulot", ProductPrice = 10_000, Quantity = 1, TotalPrice = 10_000 }
        };

        Func<Task> act = () => _service.AddAsync(dto, items);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*To'lov yetarli emas*");
    }

    [Test]
    public async Task AddAsync_NullDto_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _service.AddAsync(null!, new List<ReceiptItemDto>());
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task AddAsync_EmptyItems_ThrowsArgumentException()
    {
        var dto = new AddReceiptDto { SellerId = 1, PaidCash = 1_000 };
        Func<Task> act = () => _service.AddAsync(dto, new List<ReceiptItemDto>());
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Savat bo'sh*");
    }

    [Test]
    public async Task AddAsync_ReducesProductAmount()
    {
        var dto = new AddReceiptDto { SellerId = 1, PaidCash = 100_000 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "Test", ProductPrice = 10_000, Quantity = 3, TotalPrice = 30_000 }
        };
        var savedReceipt = new Receipt { Id = 1, SellerId = 1, TotalPrice = 30_000 };
        var product = new Product { Id = 1, Amount = 20 };

        _receiptRepoMock.Setup(r => r.AddAsync(It.IsAny<Receipt>())).ReturnsAsync(savedReceipt);
        _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        await _service.AddAsync(dto, items);

        _productRepoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.Amount == 17)), Times.Once);
    }
}
