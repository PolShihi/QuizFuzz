using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Сущность пользователя
/// </summary>
public class User : BaseEntity, IAggregateRoot
{
    public string Username { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsBanned { get; private set; }
    public DateTime? BannedUntil { get; private set; }

    private readonly List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    // Navigation properties
    private readonly List<Question> _questions = new();
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    private User() { } // EF Core

    public User(string username, Email email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        if (username.Length < 3)
            throw new ArgumentException("Username must be at least 3 characters", nameof(username));

        Username = username;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        IsBanned = false;

        _roles.Add(UserRole.User); // Default role
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Ban(DateTime? until = null)
    {
        if (until.HasValue && until.Value <= DateTime.UtcNow)
            throw new ArgumentException("Ban expiration date must be in the future", nameof(until));

        IsBanned = true;
        BannedUntil = until;
    }

    public bool IsBanActive(DateTime? utcNow = null)
    {
        if (!IsBanned)
            return false;

        var now = utcNow ?? DateTime.UtcNow;
        return !BannedUntil.HasValue || BannedUntil.Value > now;
    }

    public void Unban()
    {
        IsBanned = false;
        BannedUntil = null;
    }

    public void AddRole(UserRole role)
    {
        if (!_roles.Contains(role))
        {
            _roles.Add(role);
        }
    }

    public void RemoveRole(UserRole role)
    {
        if (role == UserRole.User && _roles.Count == 1)
            throw new InvalidOperationException("Cannot remove the last User role");

        _roles.Remove(role);
    }

    public void SetRoles(IEnumerable<UserRole> roles)
    {
        if (roles == null)
            throw new ArgumentNullException(nameof(roles));

        var normalizedRoles = roles
            .Distinct()
            .ToList();

        if (!normalizedRoles.Contains(UserRole.User))
            normalizedRoles.Insert(0, UserRole.User);

        _roles.Clear();
        _roles.AddRange(normalizedRoles);
    }

    public bool HasRole(UserRole role) => _roles.Contains(role);

    public bool IsAdmin => _roles.Contains(UserRole.Admin);
    public bool IsModerator => _roles.Contains(UserRole.Moderator);

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
    }

    public void UpdateUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            throw new ArgumentException("Username cannot be empty", nameof(newUsername));

        if (newUsername.Length < 3)
            throw new ArgumentException("Username must be at least 3 characters", nameof(newUsername));

        Username = newUsername;
    }
}
