using FluentValidation;
using POS.Application.Common.DataTransferObjects.ReceiptDtos;

namespace POS.Application.Common.Validators;

public class AddReceiptDtoValidator : AbstractValidator<AddReceiptDto>
{
    public AddReceiptDtoValidator()
    {
        RuleFor(x => x.SellerId)
            .GreaterThan(0).WithMessage("Seller ID noto'g'ri");

        RuleFor(x => x.PaidCash)
            .GreaterThanOrEqualTo(0).WithMessage("Naqd to'lov manfiy bo'lishi mumkin emas");

        RuleFor(x => x.PaidCard)
            .GreaterThanOrEqualTo(0).WithMessage("Karta to'lovi manfiy bo'lishi mumkin emas");

        RuleFor(x => x)
            .Must(x => x.PaidCash + x.PaidCard > 0)
            .WithMessage("Kamida bir to'lov usuli tanlanishi kerak");
    }
}
