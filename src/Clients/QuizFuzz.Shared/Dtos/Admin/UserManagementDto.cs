namespace QuizFuzz.Shared.Dtos.Admin;

public class UserManagementDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}

public class UpdateUserRolesRequest
{
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class BanUserRequest
{
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? BannedUntil { get; set; }
}
