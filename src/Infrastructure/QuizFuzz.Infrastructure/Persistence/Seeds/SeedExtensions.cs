using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace QuizFuzz.Infrastructure.Persistence.Seeds;

/// <summary>
/// Extension методы для seeding базы данных
/// </summary>
public static class SeedExtensions
{
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = services.GetRequiredService<Application.Common.Interfaces.Services.IPasswordHasher>();
            var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

            var seeder = new DatabaseSeeder(context, passwordHasher, logger);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DatabaseSeeding");
            logger.LogDebug(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
