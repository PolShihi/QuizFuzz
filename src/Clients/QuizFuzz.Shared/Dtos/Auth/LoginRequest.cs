using System.ComponentModel.DataAnnotations;
using QuizFuzz.Shared.Localization;

namespace QuizFuzz.Shared.Dtos.Auth;

public class LoginRequest
{
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailOrUsernameRequired))]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordRequired))]
    [MinLength(6, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordMinLength))]
    public string Password { get; set; } = string.Empty;
}
