namespace QuizFuzz.Shared.Dtos.Auth;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
