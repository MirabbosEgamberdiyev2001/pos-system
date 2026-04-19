using POS.Application.Common.Models;
using POS.Domain.Entities.Selling;

namespace POS.Application.Common.DataTransferObjects.TransactionDtos;

public class TransactionDto : BaseModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public int ReceiptId { get; set; }

    public static implicit operator TransactionDto(Transaction t)
        => new()
        {
            Id = t.Id,
            ProductId = t.ProductId,
            ProductName = t.ProductName,
            ProductPrice = t.ProductPrice,
            Quantity = t.Quantity,
            TotalPrice = t.TotalPrice,
            ReceiptId = t.ReceiptId
        };
}
