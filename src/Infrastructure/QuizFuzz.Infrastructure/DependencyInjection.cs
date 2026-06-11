using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Infrastructure.Persistence;
using QuizFuzz.Infrastructure.Services;
using QuizFuzz.Infrastructure.Services.FuzzyMatching;
using QuizFuzz.Infrastructure.Services.Email;

namespace QuizFuzz.Infrastructure;

/// <summary>
/// Регистрация зависимостей Infrastructure Layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Unit of Work and Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        var emailSection = configuration.GetSection("Email");
        services.Configure<EmailOptions>(options =>
        {
            options.Mode = emailSection["Mode"] ?? options.Mode;
            options.FromName = emailSection["FromName"] ?? options.FromName;
            options.FromAddress = emailSection["FromAddress"] ?? options.FromAddress;
            options.SmtpHost = emailSection["SmtpHost"] ?? options.SmtpHost;
            options.SmtpUsername = emailSection["SmtpUsername"] ?? options.SmtpUsername;
            options.SmtpPassword = emailSection["SmtpPassword"] ?? options.SmtpPassword;
            options.EnableSsl = bool.TryParse(emailSection["EnableSsl"], out var enableSsl)
                ? enableSsl
                : options.EnableSsl;
            options.SmtpPort = int.TryParse(emailSection["SmtpPort"], out var smtpPort)
                ? smtpPort
                : options.SmtpPort;
        });
        services.AddScoped<IFuzzyMatchingService, FuzzyMatchingService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}
