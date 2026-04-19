using Microsoft.Extensions.Logging;
using POS.Domain.DataContext;
using POS.Domain.Entities.Auth;
using POS.Domain.Enums;

namespace POS.Application.Services;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void Seed()
    {
        SeedSuperAdmin();
    }

    private void SeedSuperAdmin()
    {
        if (_context.Users.Any(u => u.Role == Role.SuperAdmin))
            return;

        // Default parol: Admin.123$ — birinchi kirishda o'zgartirilishi lozim
        var hash = BCrypt.Net.BCrypt.HashPassword("Admin.123$", workFactor: 11);

        _context.Users.Add(new User
        {
            FirstName = "Super",
            LastName = "Admin",
            PhoneNumber = "998901234567",
            IsDeleted = false,
            LastModifiedDate = DateTime.UtcNow,
            PasswordHash = hash,
            Role = Role.SuperAdmin
        });

        _context.SaveChanges();
        _logger.LogInformation("SuperAdmin foydalanuvchi yaratildi");
    }
}
