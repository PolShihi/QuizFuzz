using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Auth;

public class ResendRegistrationCodeRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
