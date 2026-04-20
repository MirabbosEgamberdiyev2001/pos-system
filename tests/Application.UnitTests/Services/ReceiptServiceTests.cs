using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
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
    private Mock<IUnitOfWork>         _unitOfWorkMock;
    private Mock<IReceiptInterface>   _receiptRepoMock;
    private Mock<ITransactionInterface> _transactionRepoMock;
    private Mock<IProductInterface>   _productRepoMock;
    private Mock<IDbContextTransaction> _transactionMock;
    private ReceiptService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock      = new Mock<IUnitOfWork>();
        _receiptRepoMock     = new Mock<IReceiptInterface>();
        _transactionRepoMock = new Mock<ITransactionInterface>();
        _productRepoMock     = new Mock<IProductInterface>();
        _transactionMock     = new Mock<IDbContextTransaction>();

        _unitOfWorkMock.Setup(u => u.Receipts).Returns(_receiptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync(default))
            .ReturnsAsync(_transactionMock.Object);
        _transactionMock.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);
        _transactionMock.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);

        _service = new ReceiptService(_unitOfWorkMock.Object, NullLogger<ReceiptService>.Instance);
    }

    private void SetupProduct(int id, decimal amount)
    {
        var product = new Product { Id = id, Amount = amount };
        _productRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.UpdateAsync(It.Is<Product>(p => p.Id == id)))
                        .ReturnsAsync((Product p) => p);
    }

    // ─── Muvaffaqiyatli operatsiyalar ────────────────────────────────────

    [Test]
    public async Task AddAsync_ValidData_CreatesReceiptAndTransactions()
    {
        var dto   = new AddReceiptDto { SellerId = 1, PaidCash = 50_000 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "Olma", ProductPrice = 5_000, Quantity = 5, TotalPrice = 25_000 },
            new() { ProductId = 2, ProductName = "Non",  ProductPrice = 2_500, Quantity = 4, TotalPrice = 10_000 }
        };
        var savedReceipt = new Receipt { Id = 10, SellerId = 1, TotalPrice = 35_000 };

        _receiptRepoMock.Setup(r => r.AddAsync(It.IsAny<Receipt>())).ReturnsAsync(savedReceipt);
        _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                            .ReturnsAsync((Transaction t) => t);
        SetupProduct(1, 100);
        SetupProduct(2, 50);

        var result = await _service.AddAsync(dto, items);

        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        _transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Test]
    public async Task AddAsync_ReducesProductAmount()
    {
        var dto   = new AddReceiptDto { SellerId = 1, PaidCash = 100_000 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "Test", ProductPrice = 10_000, Quantity = 3, TotalPrice = 30_000 }
        };
        var savedReceipt = new Receipt { Id = 1, SellerId = 1, TotalPrice = 30_000 };

        _receiptRepoMock.Setup(r => r.AddAsync(It.IsAny<Receipt>())).ReturnsAsync(savedReceipt);
        _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                            .ReturnsAsync((Transaction t) => t);
        SetupProduct(1, 20);

        await _service.AddAsync(dto, items);

        _productRepoMock.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.Amount == 17)), Times.Once);
    }

    // ─── Xato holatlari ───────────────────────────────────────────────────

    [Test]
    public async Task AddAsync_InsufficientPayment_Throws()
    {
        var dto   = new AddReceiptDto { SellerId = 1, PaidCash = 1_000 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "X", ProductPrice = 10_000, Quantity = 1, TotalPrice = 10_000 }
        };
        SetupProduct(1, 100);

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
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Savat bo'sh*");
    }

    [Test]
    public async Task AddAsync_InsufficientStock_Throws()
    {
        var dto   = new AddReceiptDto { SellerId = 1, PaidCash = 100_000 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "Product", ProductPrice = 1_000, Quantity = 50, TotalPrice = 50_000 }
        };
        SetupProduct(1, 5); // stokda 5 ta, so'rov 50 ta

        Func<Task> act = () => _service.AddAsync(dto, items);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*stokda yetarli emas*");
    }

    [Test]
    public async Task AddAsync_OnException_RollbackCalled()
    {
        var dto   = new AddReceiptDto { SellerId = 1, PaidCash = 100_000 };
        var items = new List<ReceiptItemDto>
        {
            new() { ProductId = 1, ProductName = "X", ProductPrice = 10_000, Quantity = 2, TotalPrice = 20_000 }
        };
        SetupProduct(1, 100);

        _receiptRepoMock.Setup(r => r.AddAsync(It.IsAny<Receipt>()))
                        .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = () => _service.AddAsync(dto, items);

        await act.Should().ThrowAsync<Exception>();
        _transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(default), Times.Never);
    }
}
