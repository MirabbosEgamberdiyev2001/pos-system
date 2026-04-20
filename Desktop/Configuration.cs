using DataLayer.Repositories;
using Desktop.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Application;
using POS.Application.Interfaces;
using POS.Domain.Interfaces;
using POS.Domain.Repositories;
using Serilog;

namespace Desktop;

/// <summary>
/// Legacy static service provider — faqat eskilik uchun saqlanmoqda.
/// Yangi kod uchun constructor injection ishlatilsin.
/// </summary>
public static class Configuration
{
    private static IServiceProvider? _cachedProvider;

    public static IServiceProvider GetServiceProvider()
    {
        if (_cachedProvider != null) return _cachedProvider;

        var configPath = AppDomain.CurrentDomain.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        _cachedProvider = services.BuildServiceProvider();
        return _cachedProvider;
    }

    private static void ConfigureServices(ServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=PosDB;Trusted_Connection=True;MultipleActiveResultSets=true";

        services.AddSingleton<IConfiguration>(configuration);

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

        // Application services (AutoMapper, FluentValidation, MemoryCache, barcha servicelar)
        services.AddApplicationServices();

        services.AddScoped<StartForm>();
        services.AddScoped<Login>();
    }
}
