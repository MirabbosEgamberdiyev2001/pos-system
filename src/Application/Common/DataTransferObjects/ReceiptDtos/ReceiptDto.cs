using POS.Application.Common.DataTransferObjects.TransactionDtos;
using POS.Application.Common.Models;
using POS.Domain.Entities.Selling;

namespace POS.Application.Common.DataTransferObjects.ReceiptDtos;

public class ReceiptDto : BaseModel
{
    public decimal TotalPrice { get; set; }
    public decimal PaidCash { get; set; }
    public decimal PaidCard { get; set; }
    public decimal Change => PaidCash + PaidCard - TotalPrice;
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();

    public static implicit operator ReceiptDto(Receipt receipt)
        => new()
        {
            Id = receipt.Id,
            TotalPrice = receipt.TotalPrice,
            PaidCard = receipt.PaidCard,
            PaidCash = receipt.PaidCash,
            SellerId = receipt.SellerId,
            SellerName = receipt.Seller != null
                ? $"{receipt.Seller.FirstName} {receipt.Seller.LastName}"
                : string.Empty,
            Date = receipt.LastModifiedDate
        };
}
