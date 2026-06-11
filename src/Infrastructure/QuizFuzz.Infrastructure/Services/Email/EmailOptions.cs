namespace QuizFuzz.Infrastructure.Services.Email;

public class EmailOptions
{
    public string Mode { get; set; } = "Development";
    public string FromName { get; set; } = "QuizFuzz";
    public string FromAddress { get; set; } = "no-reply@quizfuzz.local";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
