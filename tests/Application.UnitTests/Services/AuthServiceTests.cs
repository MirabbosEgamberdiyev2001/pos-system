using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using POS.Application.Common.Enums;
using POS.Application.Services;
using POS.Domain.Entities.Auth;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IUserInterface> _userRepoMock;
    private AuthService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserInterface>();
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _service = new AuthService(_unitOfWorkMock.Object, NullLogger<AuthService>.Instance);
    }

    private static User CreateUser(string phone, string plainPassword, Role role)
    {
        return new User
        {
            Id = 1,
            PhoneNumber = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 4),
            Role = role,
            FirstName = "Test",
            LastName = "User"
        };
    }

    [Test]
    public async Task LoginAsync_CorrectCredentials_ReturnsSuccess()
    {
        var user = CreateUser("998901234567", "Admin.123$", Role.SuperAdmin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "Admin.123$", UserRoles.SuperAdmin);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        var user = CreateUser("998901234567", "Admin.123$", Role.SuperAdmin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "WrongPassword", UserRoles.SuperAdmin);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Test]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        var result = await _service.LoginAsync("000000000", "any", UserRoles.Admin);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task LoginAsync_WrongRole_ReturnsAccessDenied()
    {
        var user = CreateUser("998901234567", "Admin.123$", Role.Seller);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "Admin.123$", UserRoles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Test]
    public async Task LoginAsync_SuperAdminRole_AllowsAnyUser()
    {
        var user = CreateUser("998901234567", "Admin.123$", Role.SuperAdmin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        // SuperAdmin role as parameter — always allowed for SuperAdmin users
        var result = await _service.LoginAsync("998901234567", "Admin.123$", UserRoles.SuperAdmin);

        result.IsSuccess.Should().BeTrue();
    }
}
