namespace QuizFuzz.Shared.Dtos.Auth;

public class StartRegistrationResponse
{
    public string Email { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public int ResendCooldownSeconds { get; set; }
}
