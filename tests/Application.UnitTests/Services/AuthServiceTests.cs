using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using POS.Application.Common.Enums;
using POS.Application.Interfaces;
using POS.Application.Services;
using POS.Domain.Entities.Auth;
using POS.Domain.Enums;
using POS.Domain.Interfaces;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUnitOfWork>    _unitOfWorkMock;
    private Mock<IUserInterface> _userRepoMock;
    private Mock<IPasswordHasher> _hasherMock;
    private AuthService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock   = new Mock<IUserInterface>();
        _hasherMock     = new Mock<IPasswordHasher>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        // Default: parol to'g'ri
        _hasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        _service = new AuthService(
            _unitOfWorkMock.Object,
            NullLogger<AuthService>.Instance,
            _hasherMock.Object);
    }

    private static User MakeUser(string phone, Role role) => new()
    {
        Id           = 1,
        PhoneNumber  = phone,
        PasswordHash = "hashed",
        Role         = role,
        FirstName    = "Test",
        LastName     = "User"
    };

    // ─── Mavjudlik testlari ───────────────────────────────────────────────

    [Test]
    public async Task LoginAsync_CorrectCredentials_ReturnsSuccess()
    {
        var user = MakeUser("998901234567", Role.SuperAdmin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "correct", UserRoles.SuperAdmin);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
    }

    [Test]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        var user = MakeUser("998901234567", Role.SuperAdmin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _hasherMock.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        var result = await _service.LoginAsync("998901234567", "wrong", UserRoles.SuperAdmin);

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
        var user = MakeUser("998901234567", Role.Seller);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "correct", UserRoles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Test]
    public async Task LoginAsync_SuperAdmin_LoginAsSuperAdmin_Allowed()
    {
        var user = MakeUser("998901234567", Role.SuperAdmin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "correct", UserRoles.SuperAdmin);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task LoginAsync_Admin_CanLoginAsSuperAdminRoleRequest_Denied()
    {
        // Admin foydalanuvchi SuperAdmin sifatida kirmoqchi — ruxsat yo'q
        var user = MakeUser("998901234567", Role.Admin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998901234567", "correct", UserRoles.SuperAdmin);

        result.IsSuccess.Should().BeFalse();
    }

    // ─── Brute-force protection testlari ─────────────────────────────────

    [Test]
    public async Task LoginAsync_After5FailedAttempts_AccountLocked()
    {
        // Har safar user topilmaydi — 5 ta urinish
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        const string phone = "998911111111";
        for (int i = 0; i < 5; i++)
            await _service.LoginAsync(phone, "wrong", UserRoles.Admin);

        // 6-urinish — bloklangan bo'lishi kerak
        var result = await _service.LoginAsync(phone, "correct", UserRoles.Admin);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bloklangan");
    }

    [Test]
    public async Task LoginAsync_SuccessfulLogin_ClearsFailedAttempts()
    {
        var user = MakeUser("998922222222", Role.Admin);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        // 2 ta noto'g'ri urinish
        _hasherMock.Setup(h => h.Verify("bad", "hashed")).Returns(false);
        await _service.LoginAsync("998922222222", "bad", UserRoles.Admin);
        await _service.LoginAsync("998922222222", "bad", UserRoles.Admin);

        // To'g'ri login
        _hasherMock.Setup(h => h.Verify("good", "hashed")).Returns(true);
        var result = await _service.LoginAsync("998922222222", "good", UserRoles.Admin);

        result.IsSuccess.Should().BeTrue();
    }

    // ─── Result<UserDto> testlari ─────────────────────────────────────────

    [Test]
    public async Task LoginAsync_Success_ReturnsUserDtoWithCorrectRole()
    {
        var user = MakeUser("998933333333", Role.Seller);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var result = await _service.LoginAsync("998933333333", "correct", UserRoles.Seller);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Role.Should().Be("Seller");
        result.Data.PhoneNumber.Should().Be("998933333333");
    }
}
