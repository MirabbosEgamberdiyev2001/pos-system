using FluentValidation;
using POS.Application.Common.DataTransferObjects.WarehouseItemDtos;

namespace POS.Application.Common.Validators;

public class AddProductItemDtoValidator : AbstractValidator<AddProductItemDto>
{
    public AddProductItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Mahsulot tanlanishi kerak");

        RuleFor(x => x.AdminId)
            .GreaterThan(0).WithMessage("Admin ID noto'g'ri");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Miqdor noldan katta bo'lishi kerak");

        RuleFor(x => x.BuyingPrice)
            .GreaterThan(0).WithMessage("Xarid narxi noldan katta bo'lishi kerak");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("Sotuv narxi noldan katta bo'lishi kerak")
            .GreaterThanOrEqualTo(x => x.BuyingPrice)
                .WithMessage("Sotuv narxi xarid narxidan past bo'lishi mumkin emas");
    }
}
