using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Validators;
using POS.Application.Interfaces;
using POS.Application.Mappings;
using POS.Application.Services;

namespace POS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<AddCategoryDtoValidator>();

        // In-memory cache (CategoryService, ProductService uchun)
        services.AddMemoryCache();

        // Services
        services.AddTransient<ICategoryService, CategoryService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<IProductItemService, ProductItemService>();
        services.AddTransient<IReceiptService, ReceiptService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IBusinessUnit, BusinessUnit>();
        services.AddTransient<DatabaseSeeder>();

        return services;
    }
}
