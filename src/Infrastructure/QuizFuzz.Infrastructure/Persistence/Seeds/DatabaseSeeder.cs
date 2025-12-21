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
        await SeedQuestionsAsync();

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

    private async Task SeedQuestionsAsync()
    {
        if (await _context.Questions.AnyAsync())
        {
            _logger.LogInformation("Questions already exist, skipping...");
            return;
        }

        var tags = await _context.Tags.ToListAsync();
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found, cannot seed questions");
            return;
        }

        // Вопросы с тегами
        var questionsWithTags = new[]
        {
            // НАУКА
            new 
            { 
                Text = "Какая планета Солнечной системы самая большая?", 
                Answer = "Юпитер",
                Aliases = new[] { "Jupiter", "юпитер" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "Наука", "Космос" }
            },
            new 
            { 
                Text = "Сколько элементов в периодической таблице Менделеева? (укажите число)", 
                Answer = "118",
                Aliases = new[] { "сто восемнадцать", "118 элементов" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Наука" }
            },
            new 
            { 
                Text = "Как называется процесс превращения воды в пар?", 
                Answer = "Испарение",
                Aliases = new[] { "парообразование", "выпаривание", "evaporation" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "Наука" }
            },

            // ГЕОГРАФИЯ
            new 
            { 
                Text = "Какая самая длинная река в мире?", 
                Answer = "Амазонка",
                Aliases = new[] { "Amazon", "Амазония", "река Амазонка" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "География", "Природа" }
            },
            new 
            { 
                Text = "В какой стране находится Эйфелева башня?", 
                Answer = "Франция",
                Aliases = new[] { "France", "франция", "во Франции" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "География" }
            },
            new 
            { 
                Text = "Какая столица Японии?", 
                Answer = "Токио",
                Aliases = new[] { "Tokyo", "токио" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "География" }
            },

            // ИСТОРИЯ
            new 
            { 
                Text = "В каком году закончилась Вторая мировая война?", 
                Answer = "1945",
                Aliases = new[] { "тысяча девятьсот сорок пятом", "45-м", "1945 году" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "История" }
            },
            new 
            { 
                Text = "Кто открыл Америку?", 
                Answer = "Христофор Колумб",
                Aliases = new[] { "Колумб", "Columbus", "Christopher Columbus", "Христофор Колумб" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "История", "География" }
            },

            // ТЕХНОЛОГИИ
            new 
            { 
                Text = "Кто является основателем компании Microsoft?", 
                Answer = "Билл Гейтс",
                Aliases = new[] { "Bill Gates", "Гейтс", "Уильям Гейтс", "William Gates" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Технологии" }
            },
            new 
            { 
                Text = "Что означает аббревиатура CPU?", 
                Answer = "Central Processing Unit",
                Aliases = new[] { "Центральный процессор", "процессор", "ЦП", "центральное процессорное устройство" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Технологии" }
            },

            // КИНО
            new 
            { 
                Text = "Кто режиссер фильма 'Титаник'?", 
                Answer = "Джеймс Кэмерон",
                Aliases = new[] { "James Cameron", "Кэмерон", "Джеймс Камерон", "Cameron" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Кино" }
            },
            new 
            { 
                Text = "Как зовут главного героя фильма 'Матрица'?", 
                Answer = "Нео",
                Aliases = new[] { "Neo", "Томас Андерсон", "Thomas Anderson", "Мистер Андерсон" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "Кино" }
            },

            // СПОРТ
            new 
            { 
                Text = "Сколько игроков в футбольной команде на поле?", 
                Answer = "11",
                Aliases = new[] { "одиннадцать", "11 игроков", "одиннадцать человек" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "Спорт" }
            },
            new 
            { 
                Text = "В каком городе проходили первые современные Олимпийские игры?", 
                Answer = "Афины",
                Aliases = new[] { "Athens", "Афинах", "в Афинах" },
                Difficulty = Domain.Enums.Difficulty.Hard,
                TagNames = new[] { "Спорт", "История" }
            },

            // ЛИТЕРАТУРА
            new 
            { 
                Text = "Кто написал 'Войну и мир'?", 
                Answer = "Лев Толстой",
                Aliases = new[] { "Толстой", "Leo Tolstoy", "Л.Н. Толстой", "Лев Николаевич Толстой" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Литература" }
            },
            new 
            { 
                Text = "Как называется первая книга о Гарри Поттере?", 
                Answer = "Гарри Поттер и философский камень",
                Aliases = new[] { "Философский камень", "Harry Potter and the Philosopher's Stone", "Harry Potter and the Sorcerer's Stone" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Литература" }
            },

            // МУЗЫКА
            new 
            { 
                Text = "Какая группа исполняет песню 'Bohemian Rhapsody'?", 
                Answer = "Queen",
                Aliases = new[] { "Куин", "queen", "группа Queen" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Музыка" }
            },
            new 
            { 
                Text = "Сколько струн у стандартной гитары?", 
                Answer = "6",
                Aliases = new[] { "шесть", "6 струн", "шесть струн" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "Музыка" }
            }
        };

        // Вопросы БЕЗ тегов (общие знания)
        var questionsWithoutTags = new[]
        {
            new 
            { 
                Text = "Сколько дней в високосном году?", 
                Answer = "366",
                Aliases = new[] { "триста шестьдесят шесть", "366 дней" },
                Difficulty = Domain.Enums.Difficulty.Easy
            },
            new 
            { 
                Text = "Какой цвет получается при смешивании красного и желтого?", 
                Answer = "Оранжевый",
                Aliases = new[] { "orange", "оранжевый цвет" },
                Difficulty = Domain.Enums.Difficulty.Easy
            },
            new 
            { 
                Text = "Сколько сторон у шестиугольника?", 
                Answer = "6",
                Aliases = new[] { "шесть", "6 сторон", "шесть сторон" },
                Difficulty = Domain.Enums.Difficulty.Easy
            },
            new 
            { 
                Text = "Как называется боязнь пауков?", 
                Answer = "Арахнофобия",
                Aliases = new[] { "arachnophobia", "арахнофобия" },
                Difficulty = Domain.Enums.Difficulty.Hard
            },
            new 
            { 
                Text = "Сколько минут в трех часах?", 
                Answer = "180",
                Aliases = new[] { "сто восемьдесят", "180 минут" },
                Difficulty = Domain.Enums.Difficulty.Easy
            }
        };

        var questionsList = new List<Question>();

        // Создаем вопросы С ТЕГАМИ
        foreach (var q in questionsWithTags)
        {
            var question = new Question(
                Domain.Enums.QuestionType.Text,
                q.Text,
                q.Difficulty,
                "ru",
                null,
                adminUser.Id
            );
            
            // Добавляем канонический ответ через метод AddAnswer
            var primaryAnswer = question.AddAnswer(q.Answer, isPrimary: true, languageCode: "ru");

            // Добавляем алиасы (альтернативные ответы)
            foreach (var alias in q.Aliases)
            {
                primaryAnswer.AddAlias(alias, Domain.Enums.AliasKind.Synonym);
            }

            // Добавляем теги
            foreach (var tagName in q.TagNames)
            {
                var tag = tags.FirstOrDefault(t => t.Name == tagName);
                if (tag != null)
                {
                    question.AddTag(tag);
                }
            }

            // Отправляем на ревью и одобряем
            question.SubmitForReview();
            question.Approve();

            questionsList.Add(question);
        }

        // Создаем вопросы БЕЗ ТЕГОВ
        foreach (var q in questionsWithoutTags)
        {
            var question = new Question(
                Domain.Enums.QuestionType.Text,
                q.Text,
                q.Difficulty,
                "ru",
                null,
                adminUser.Id
            );
            
            // Добавляем канонический ответ через метод AddAnswer
            var primaryAnswer = question.AddAnswer(q.Answer, isPrimary: true, languageCode: "ru");

            // Добавляем алиасы
            foreach (var alias in q.Aliases)
            {
                primaryAnswer.AddAlias(alias, Domain.Enums.AliasKind.Synonym);
            }

            // Отправляем на ревью и одобряем
            question.SubmitForReview();
            question.Approve();

            questionsList.Add(question);
        }

        await _context.Questions.AddRangeAsync(questionsList);
        _logger.LogInformation("Seeded {Count} questions ({WithTags} with tags, {WithoutTags} without tags)", 
            questionsList.Count, 
            questionsWithTags.Length, 
            questionsWithoutTags.Length);
    }
}
