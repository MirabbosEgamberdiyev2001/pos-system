using FluentValidation;
using POS.Application.Common.DataTransferObjects.CategoryDtos;

namespace POS.Application.Common.Validators;

public class AddCategoryDtoValidator : AbstractValidator<AddCategoryDto>
{
    public AddCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategoriya nomi bo'sh bo'lishi mumkin emas")
            .MinimumLength(2).WithMessage("Kategoriya nomi kamida 2 ta belgidan iborat bo'lishi kerak")
            .MaximumLength(100).WithMessage("Kategoriya nomi 100 ta belgidan oshmasligi kerak");
    }
}
