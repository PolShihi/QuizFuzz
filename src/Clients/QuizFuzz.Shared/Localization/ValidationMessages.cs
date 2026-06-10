using System.Globalization;

namespace QuizFuzz.Shared.Localization;

public static class ValidationMessages
{
    private static bool IsRussian => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);

    public static string EmailOrUsernameRequired => IsRussian ? "Введите email или имя пользователя." : "Email or username is required.";
    public static string PasswordRequired => IsRussian ? "Введите пароль." : "Password is required.";
    public static string PasswordMinLength => IsRussian ? "Пароль должен содержать минимум 6 символов." : "Password must be at least 6 characters.";
    public static string UsernameRequired => IsRussian ? "Введите имя пользователя." : "Username is required.";
    public static string UsernameMinLength => IsRussian ? "Имя пользователя должно содержать минимум 3 символа." : "Username must be at least 3 characters.";
    public static string UsernameMaxLength => IsRussian ? "Имя пользователя не может быть длиннее 50 символов." : "Username cannot exceed 50 characters.";
    public static string EmailRequired => IsRussian ? "Введите email." : "Email is required.";
    public static string InvalidEmailFormat => IsRussian ? "Введите корректный email." : "Invalid email format.";
    public static string ConfirmPasswordRequired => IsRussian ? "Подтвердите пароль." : "Confirm password is required.";
    public static string PasswordsDoNotMatch => IsRussian ? "Пароли не совпадают." : "Passwords do not match.";
}
