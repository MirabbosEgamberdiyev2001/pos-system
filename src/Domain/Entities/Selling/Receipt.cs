using System.ComponentModel.DataAnnotations;
using POS.Domain.Common;
using POS.Domain.Entities.Auth;

namespace POS.Domain.Entities.Selling;

public class Receipt : BaseEntity
{
    [Required]
    public decimal TotalPrice { get; set; }
    [Required]
    public decimal PaidCash { get; set; }
    [Required]
    public decimal PaidCard { get; set; }

    [Required]
    public int SellerId { get; set; }
    public User? Seller { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
