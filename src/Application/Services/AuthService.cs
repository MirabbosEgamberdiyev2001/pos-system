using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using POS.Application.Common.DataTransferObjects.UserDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Models;
using POS.Application.Interfaces;
using POS.Domain.Entities.Auth;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher _passwordHasher;

    // Brute-force protection: phone -> (attempts, lastAttemptTime)
    private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)>
        _loginAttempts = new();

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthService(IUnitOfWork unitOfWork,
                       ILogger<AuthService> logger,
                       IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserDto>> LoginAsync(string phoneNumber, string password, UserRoles role)
    {
        try
        {
            // --- Brute-force tekshirish ---
            if (_loginAttempts.TryGetValue(phoneNumber, out var attempt))
            {
                if (attempt.Count >= MaxFailedAttempts &&
                    DateTime.UtcNow - attempt.LastAttempt < LockoutDuration)
                {
                    var remaining = LockoutDuration - (DateTime.UtcNow - attempt.LastAttempt);
                    _logger.LogWarning("Akkaunt bloklangan: {Phone} — {Remaining:mm\\:ss} qoldi", phoneNumber, remaining);
                    return Result<UserDto>.Failure(
                        $"Akkaunt {remaining.Minutes} daqiqa {remaining.Seconds} soniya bloklangan.");
                }

                // Lockout muddati o'tgan bo'lsa — tozalash
                if (DateTime.UtcNow - attempt.LastAttempt >= LockoutDuration)
                    _loginAttempts.TryRemove(phoneNumber, out _);
            }

            // --- Foydalanuvchi qidirish ---
            var users = await _unitOfWork.Users.GetAllAsync();
            var user = users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                RecordFailedAttempt(phoneNumber);
                _logger.LogWarning("Login xatoligi: {Phone} topilmadi", phoneNumber);
                return Result<UserDto>.Failure(ErrorMessages.USER_NOT_FOUND);
            }

            // --- Parol tekshirish ---
            if (!_passwordHasher.Verify(password, user.PasswordHash))
            {
                RecordFailedAttempt(phoneNumber);
                _logger.LogWarning("Login xatoligi: {Phone} — noto'g'ri parol", phoneNumber);
                return Result<UserDto>.Failure(ErrorMessages.LOGIN_FAILED);
            }

            // --- Rol tekshirish ---
            if (!IsInRole(user, role))
            {
                RecordFailedAttempt(phoneNumber);
                _logger.LogWarning("Ruxsat yo'q: {Phone} roli {Role} uchun kirmoqchi", phoneNumber, role);
                return Result<UserDto>.Failure(ErrorMessages.ACCESS_DENIED);
            }

            // --- Muvaffaqiyatli login ---
            _loginAttempts.TryRemove(phoneNumber, out _);
            _logger.LogInformation("Muvaffaqiyatli login: {Phone}, rol: {Role}", phoneNumber, role);

            var dto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString()
            };
            return Result<UserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login jarayonida kutilmagan xato: {Phone}", phoneNumber);
            return Result<UserDto>.Failure("Tizimda xatolik yuz berdi. Qayta urinib ko'ring.");
        }
    }

    private static void RecordFailedAttempt(string phone)
    {
        _loginAttempts.AddOrUpdate(
            phone,
            (1, DateTime.UtcNow),
            (_, existing) => (existing.Count + 1, DateTime.UtcNow));
    }

    private static bool IsInRole(User user, UserRoles requestedRole)
        => requestedRole switch
        {
            UserRoles.SuperAdmin => user.Role == Role.SuperAdmin,
            UserRoles.Admin      => user.Role == Role.Admin || user.Role == Role.SuperAdmin,
            UserRoles.Seller     => user.Role == Role.Seller,
            _                    => false
        };
}
