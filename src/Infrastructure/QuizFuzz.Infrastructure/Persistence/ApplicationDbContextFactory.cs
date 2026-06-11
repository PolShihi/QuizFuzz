using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuizFuzz.Infrastructure.Persistence;

/// <summary>
/// Creates ApplicationDbContext for EF Core design-time commands without starting the Web API host.
/// This avoids HostAbortedException during dotnet ef database update/migrations.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DefaultConnectionName = "DefaultConnection";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        foreach (var filePath in GetCandidateAppSettingsFiles())
        {
            var connectionString = TryReadConnectionString(filePath);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
        }

        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' was not found. " +
            "Set environment variable 'ConnectionStrings__DefaultConnection' or run dotnet ef from the solution/project folder where src/Web/QuizFuzz.Web.Api/appsettings.json is available.");
    }

    private static IEnumerable<string> GetCandidateAppSettingsFiles()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "appsettings.Development.json");
            yield return Path.Combine(current.FullName, "appsettings.json");
            yield return Path.Combine(current.FullName, "src", "Web", "QuizFuzz.Web.Api", "appsettings.Development.json");
            yield return Path.Combine(current.FullName, "src", "Web", "QuizFuzz.Web.Api", "appsettings.json");

            current = current.Parent;
        }
    }

    private static string? TryReadConnectionString(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
            {
                return null;
            }

            if (!connectionStrings.TryGetProperty(DefaultConnectionName, out var defaultConnection))
            {
                return null;
            }

            return defaultConnection.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
