using Microsoft.Extensions.Logging;
using POS.Application.Common.Enums;
using POS.Application.Common.Models;
using POS.Application.Interfaces;
using POS.Domain.Entities.Auth;
using POS.Domain.Interfaces;

namespace POS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> LoginAsync(string phoneNumber, string password, UserRoles role)
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var user = users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: user with phone {Phone} not found", phoneNumber);
                return new Result(false, ErrorMessages.USER_NOT_FOUND);
            }

            if (!PasswordEncoder.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt failed: wrong password for phone {Phone}", phoneNumber);
                return new Result(false, ErrorMessages.LOGIN_FAILED);
            }

            if (!IsInRole(user, role) && role != UserRoles.SuperAdmin)
            {
                _logger.LogWarning("Access denied for user {Phone} with role {Role}", phoneNumber, role);
                return new Result(false, ErrorMessages.ACCESS_DENIED);
            }

            _logger.LogInformation("User {Phone} logged in successfully with role {Role}", phoneNumber, role);
            return new Result();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Phone}", phoneNumber);
            return new Result(false, "Tizimda xatolik yuz berdi");
        }
    }

    private static bool IsInRole(User user, UserRoles role)
        => user.Role.ToString().Equals(role.ToString());
}
