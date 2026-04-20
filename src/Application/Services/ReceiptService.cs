using Microsoft.Extensions.Logging;
using POS.Application.Common.DataTransferObjects.ReceiptDtos;
using POS.Application.Common.DataTransferObjects.TransactionDtos;
using POS.Application.Common.Models;
using POS.Application.Interfaces;
using POS.Domain.Entities.Selling;
using POS.Domain.Interfaces;

namespace POS.Application.Services;

public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(IUnitOfWork unitOfWork, ILogger<ReceiptService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReceiptDto> AddAsync(AddReceiptDto receiptDto, List<ReceiptItemDto> items)
    {
        if (receiptDto == null) throw new ArgumentNullException(nameof(receiptDto));
        if (items == null || items.Count == 0)
            throw new ArgumentException("Savat bo'sh bo'lishi mumkin emas", nameof(items));
        if (receiptDto.SellerId <= 0)
            throw new ArgumentException("Seller ID noto'g'ri", nameof(receiptDto));

        var totalCalculated = items.Sum(i => i.TotalPrice);
        var paid = receiptDto.PaidCash + receiptDto.PaidCard;
        if (paid < totalCalculated)
            throw new InvalidOperationException(
                $"To'lov yetarli emas. Kerak: {totalCalculated:N2}, To'landi: {paid:N2}");

        // --- Stok tekshirish (transactiondan oldin) ---
        foreach (var item in items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Mahsulot topilmadi: Id={item.ProductId}");
            if (product.Amount < item.Quantity)
                throw new InvalidOperationException(
                    $"'{product.Name}': stokda yetarli emas. Mavjud: {product.Amount}, Kerak: {item.Quantity}");
        }

        // --- Atomic transaction ---
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var receipt = (Receipt)receiptDto;
            receipt.TotalPrice = totalCalculated;
            var savedReceipt = await _unitOfWork.Receipts.AddAsync(receipt);
            await _unitOfWork.SaveChangesAsync(); // Receipt ID olish uchun

            foreach (var item in items)
            {
                var tx = new Transaction
                {
                    ReceiptId        = savedReceipt.Id,
                    ProductId        = item.ProductId,
                    ProductName      = item.ProductName,
                    ProductPrice     = item.ProductPrice,
                    Quantity         = item.Quantity,
                    TotalPrice       = item.TotalPrice,
                    IsDeleted        = false,
                    LastModifiedDate = LocalTime.GetUtc5Time()
                };
                await _unitOfWork.Transactions.AddAsync(tx);

                // Stokni kamaytirish
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.Amount = Math.Max(0, product.Amount - item.Quantity);
                    await _unitOfWork.Products.UpdateAsync(product);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Chek yaratildi: ReceiptId={Id}, SellerId={Seller}, Total={Total}",
                savedReceipt.Id, receiptDto.SellerId, savedReceipt.TotalPrice);

            return (ReceiptDto)savedReceipt;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Chek yaratishda xato — rollback bajarildi");
            throw;
        }
    }

    public async Task<IEnumerable<ReceiptDto>> GetAllAsync()
    {
        var receipts = await _unitOfWork.Receipts.GetAllAsync();
        return receipts.Select(r => (ReceiptDto)r);
    }

    public async Task<ReceiptDto?> GetByIdAsync(int id)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdAsync(id);
        return receipt == null ? null : (ReceiptDto)receipt;
    }
}
