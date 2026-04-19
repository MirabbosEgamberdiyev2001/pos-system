using FluentValidation;
using POS.Application.Common.DataTransferObjects.ProductDtos;

namespace POS.Application.Common.Validators;

public class AddProductDtoValidator : AbstractValidator<AddProductDto>
{
    public AddProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mahsulot nomi bo'sh bo'lishi mumkin emas")
            .MaximumLength(100).WithMessage("Mahsulot nomi 100 ta belgidan oshmasligi kerak");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Kategoriya tanlanishi kerak");

        RuleFor(x => x.WarningAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Ogohlantirish miqdori musbat bo'lishi kerak");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("Tavsif 200 ta belgidan oshmasligi kerak");

        RuleFor(x => x.Barcode)
            .MaximumLength(20).WithMessage("Barcode 20 ta belgidan oshmasligi kerak")
            .Matches(@"^\d*$").WithMessage("Barcode faqat raqamlardan iborat bo'lishi kerak")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}
