using DataLayer.Repositories;
using Desktop.Admin;
using Desktop.Admin.CategoryForms;
using Desktop.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Application;
using POS.Application.Services;
using POS.Infrastructure;
using POS.Domain.DataContext;
using POS.Domain.Interfaces;
using POS.Domain.Repositories;
using Serilog;

namespace Desktop;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // --- Global exception handling ---
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // --- Configuration ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // --- Serilog ---
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        POS.Infrastructure.Logging.SerilogConfigurer.Configure(logDir);

        try
        {
            Log.Information("POS dasturi ishga tushmoqda...");

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider();

            // --- Migrations va Seed ---
            ApplyMigrationsAndSeed(serviceProvider);

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = serviceProvider.GetRequiredService<AdminForm>();
            Application.Run(form);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Dastur kutilmagan xato sababli to'xtatildi");
            MessageBox.Show($"Kritik xato: {ex.Message}\n\nLog faylini ko'ring.",
                "Kritik xato", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(ServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("'Default' connection string topilmadi (appsettings.json).");

        // DbContext — Scoped: bir scope ichida bitta instance
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure()),
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Repositories
        services.AddTransient<ICategoryInterface, CategoryRepository>();
        services.AddTransient<IProductInterface, ProductRepository>();
        services.AddTransient<IProductItemInterface, ProductItemRepository>();
        services.AddTransient<IReceiptInterface, ReceiptRepository>();
        services.AddTransient<ITransactionInterface, TransactionRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddTransient<IUserInterface, UserRepository>();

        // Application services + FluentValidation
        services.AddApplicationServices();

        // Infrastructure services (BCrypt hasher, kelajakda: email/sms)
        services.AddInfrastructureServices();

        // Forms
        services.AddScoped<StartForm>();
        services.AddScoped<Login>();
        services.AddScoped<AdminForm>();
        services.AddScoped<AddCategoryForm>();
    }

    private static void ApplyMigrationsAndSeed(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Database migration boshlanmoqda...");
            db.Database.Migrate();
            logger.LogInformation("Database migration muvaffaqiyatli tugadi");

            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            seeder.Seed();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration yoki seed jarayonida xato");
            throw;
        }
    }

    private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UI thread xatosi");
        MessageBox.Show($"Xatolik yuz berdi:\n{e.Exception.Message}\n\nIlova davom etmoqda.",
            "Xato", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled exception — ilova to'xtatilmoqda: {IsTerminating}", e.IsTerminating);
    }
}
