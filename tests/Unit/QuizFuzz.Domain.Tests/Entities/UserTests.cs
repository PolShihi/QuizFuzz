using FluentAssertions;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var username = "testuser";
        var email = Email.Create("test@example.com");
        var passwordHash = "hashedpassword";

        // Act
        var user = new User(username, email, passwordHash);

        // Assert
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.IsBanned.Should().BeFalse();
        user.Roles.Should().Contain(UserRole.User);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ab")] // Too short
    public void Constructor_WithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var passwordHash = "hashedpassword";

        // Act
        Action act = () => new User(invalidUsername, email, passwordHash);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddRole_ShouldAddRoleToUser()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.AddRole(UserRole.Moderator);

        // Assert
        user.Roles.Should().Contain(UserRole.Moderator);
        user.IsModerator.Should().BeTrue();
    }

    [Fact]
    public void AddRole_WhenRoleAlreadyExists_ShouldNotDuplicate()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(UserRole.Moderator);

        // Act
        user.AddRole(UserRole.Moderator);

        // Assert
        user.Roles.Count(r => r == UserRole.Moderator).Should().Be(1);
    }

    [Fact]
    public void RemoveRole_ShouldRemoveRoleFromUser()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(UserRole.Moderator);

        // Act
        user.RemoveRole(UserRole.Moderator);

        // Assert
        user.Roles.Should().NotContain(UserRole.Moderator);
    }

    [Fact]
    public void RemoveRole_WhenLastUserRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        Action act = () => user.RemoveRole(UserRole.User);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot remove the last User role*");
    }

    [Fact]
    public void Ban_ShouldSetUserAsBanned()
    {
        // Arrange
        var user = CreateTestUser();
        var bannedUntil = DateTime.UtcNow.AddDays(7);

        // Act
        user.Ban(bannedUntil);

        // Assert
        user.IsBanned.Should().BeTrue();
        user.BannedUntil.Should().Be(bannedUntil);
    }

    [Fact]
    public void Ban_WithoutDate_ShouldSetPermanentBan()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.Ban();

        // Assert
        user.IsBanned.Should().BeTrue();
        user.BannedUntil.Should().BeNull();
    }

    [Fact]
    public void Unban_ShouldRemoveBan()
    {
        // Arrange
        var user = CreateTestUser();
        user.Ban(DateTime.UtcNow.AddDays(7));

        // Act
        user.Unban();

        // Assert
        user.IsBanned.Should().BeFalse();
        user.BannedUntil.Should().BeNull();
    }

    [Fact]
    public void UpdateLastLogin_ShouldSetLastLoginTime()
    {
        // Arrange
        var user = CreateTestUser();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        user.UpdateLastLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void ChangePassword_WithValidHash_ShouldUpdatePassword()
    {
        // Arrange
        var user = CreateTestUser();
        var newPasswordHash = "newhashedpassword";

        // Act
        user.ChangePassword(newPasswordHash);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ChangePassword_WithInvalidHash_ShouldThrowArgumentException(string invalidHash)
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        Action act = () => user.ChangePassword(invalidHash);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateEmail_WithValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        var user = CreateTestUser();
        var newEmail = Email.Create("newemail@example.com");

        // Act
        user.UpdateEmail(newEmail);

        // Assert
        user.Email.Should().Be(newEmail);
    }

    [Fact]
    public void UpdateUsername_WithValidUsername_ShouldUpdateUsername()
    {
        // Arrange
        var user = CreateTestUser();
        var newUsername = "newusername";

        // Act
        user.UpdateUsername(newUsername);

        // Assert
        user.Username.Should().Be(newUsername);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ab")]
    public void UpdateUsername_WithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        Action act = () => user.UpdateUsername(invalidUsername);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasRole_WhenUserHasRole_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(UserRole.Admin);

        // Act & Assert
        user.HasRole(UserRole.Admin).Should().BeTrue();
        user.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void HasRole_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.HasRole(UserRole.Admin).Should().BeFalse();
        user.IsAdmin.Should().BeFalse();
    }

    private static User CreateTestUser()
    {
        return new User(
            "testuser",
            Email.Create("test@example.com"),
            "hashedpassword"
        );
    }
}
