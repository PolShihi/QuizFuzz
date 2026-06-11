using System.ComponentModel.DataAnnotations;

namespace QuizFuzz.Shared.Dtos.Auth;

public class ConfirmRegistrationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Verification code must contain 6 digits")]
    public string Code { get; set; } = string.Empty;
}
