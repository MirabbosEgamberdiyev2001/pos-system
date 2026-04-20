using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using POS.Domain.DataContext;
using POS.Domain.Entities.Auth;
using POS.Domain.Enums;

namespace POS.Application.Services;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(ApplicationDbContext context,
                          ILogger<DatabaseSeeder> logger,
                          IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public void Seed()
    {
        SeedSuperAdmin();
    }

    private void SeedSuperAdmin()
    {
        if (_context.Users.Any(u => u.Role == Role.SuperAdmin))
            return;

        var phone = _configuration["Seeding:AdminPhone"]
                    ?? "998901234567";
        var rawPassword = _configuration["Seeding:AdminPassword"]
                          ?? throw new InvalidOperationException(
                              "Seeding:AdminPassword konfiguratsiyada topilmadi! " +
                              "appsettings.json yoki environment variable o'rnating.");

        var hash = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 11);

        _context.Users.Add(new User
        {
            FirstName        = "Super",
            LastName         = "Admin",
            PhoneNumber      = phone,
            IsDeleted        = false,
            LastModifiedDate = DateTime.UtcNow,
            PasswordHash     = hash,
            Role             = Role.SuperAdmin
        });

        _context.SaveChanges();
        _logger.LogInformation("SuperAdmin foydalanuvchi yaratildi: {Phone}", phone);
    }
}
