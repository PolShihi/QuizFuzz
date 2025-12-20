using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Infrastructure.Persistence.Seeds;

/// <summary>
/// Seeder для начального наполнения базы данных
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        await SeedTagsAsync();
        await SeedAdminUserAsync();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeding completed successfully!");
    }

    private async Task SeedTagsAsync()
    {
        if (await _context.Tags.AnyAsync())
        {
            _logger.LogInformation("Tags already exist, skipping...");
            return;
        }

        var tags = new[]
        {
            new Tag("Общие знания", "Вопросы на общую эрудицию"),
            new Tag("Наука", "Вопросы по естественным и точным наукам"),
            new Tag("История", "Исторические события и личности"),
            new Tag("География", "Страны, города, природа"),
            new Tag("Спорт", "Спортивные события и достижения"),
            new Tag("Кино", "Фильмы, актеры, режиссеры"),
            new Tag("Музыка", "Музыкальные исполнители и произведения"),
            new Tag("Литература", "Книги, писатели, поэты"),
            new Tag("Искусство", "Живопись, скульптура, архитектура"),
            new Tag("Технологии", "IT, гаджеты, инновации"),
            new Tag("Природа", "Животные, растения, экология"),
            new Tag("Космос", "Астрономия, космонавтика"),
            new Tag("Еда", "Кулинария, национальные кухни"),
            new Tag("Игры", "Настольные, видеоигры, головоломки"),
            new Tag("Мифология", "Мифы и легенды разных народов")
        };

        await _context.Tags.AddRangeAsync(tags);
        _logger.LogInformation("Seeded {Count} tags", tags.Length);
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Users already exist, skipping admin creation...");
            return;
        }

        var email = Email.Create("admin@quizfuzz.com");
        var passwordHash = _passwordHasher.HashPassword("Admin123!");

        var admin = new User("admin", email, passwordHash);
        admin.AddRole(Domain.Enums.UserRole.Admin);
        admin.AddRole(Domain.Enums.UserRole.Moderator);

        await _context.Users.AddAsync(admin);
        _logger.LogInformation("Created admin user: admin@quizfuzz.com");
        _logger.LogWarning("IMPORTANT: Default admin password is 'Admin123!' - CHANGE IT IMMEDIATELY!");
    }
}
