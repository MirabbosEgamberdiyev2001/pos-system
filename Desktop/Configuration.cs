using DataLayer.Repositories;
using Desktop.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Application;
using POS.Application.Interfaces;
using POS.Application.Services;
using POS.Domain.Interfaces;
using POS.Domain.Repositories;
using Serilog;

namespace Desktop;
public static class Configuration
{
    public static IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        const string connectionString = "Server=(localdb)\\mssqllocaldb;Database=PosDB;Trusted_Connection=True;MultipleActiveResultSets=true";
        services.AddDbContext<POS.Domain.DataContext.ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

        // Logging — ILogger<T> uchun zarur
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: false);
        });

        services.AddTransient<ICategoryInterface, CategoryRepository>();
        services.AddTransient<IProductInterface, ProductRepository>();
        services.AddTransient<IProductItemInterface, ProductItemRepository>();
        services.AddTransient<IReceiptInterface, ReceiptRepository>();
        services.AddTransient<ITransactionInterface, TransactionRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddTransient<IUserInterface, UserRepository>();

        // Application services (AutoMapper, FluentValidation, barcha servicelar)
        services.AddApplicationServices();

        services.AddTransient<IBusinessUnit, BusinessUnit>();

        services.AddScoped<StartForm>();
        services.AddScoped<Login>();
    }
}
