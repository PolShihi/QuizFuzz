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
        
        // 🔥 ВАЖНО: Сохраняем Tags и Admin перед созданием вопросов
        await _context.SaveChangesAsync();
        
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
            },
            
            // IMAGE ВОПРОСЫ
            new 
            { 
                Text = "Какой город вы видите на изображении?", 
                Answer = "Париж",
                Aliases = new[] { "Paris", "париж" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "География" }
            },
            new 
            { 
                Text = "Что изображено на картинке?", 
                Answer = "Эйфелева башня",
                Aliases = new[] { "Eiffel Tower", "башня Эйфеля", "Эйфелева" },
                Difficulty = Domain.Enums.Difficulty.Easy,
                TagNames = new[] { "География", "Искусство" }
            },
            
            // AUDIO ВОПРОСЫ
            new 
            { 
                Text = "Какой музыкальный инструмент вы слышите?", 
                Answer = "Фортепиано",
                Aliases = new[] { "Piano", "пианино", "рояль" },
                Difficulty = Domain.Enums.Difficulty.Medium,
                TagNames = new[] { "Музыка" }
            },
            new 
            { 
                Text = "Прослушайте мелодию. Какой это инструмент?", 
                Answer = "Гитара",
                Aliases = new[] { "Guitar", "гитара" },
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
        
        // Счетчик для индексации
        int questionIndex = 0;

        // Создаем вопросы С ТЕГАМИ
        foreach (var q in questionsWithTags)
        {
            // Определяем тип вопроса и MediaUrl на основе индекса
            Domain.Enums.QuestionType questionType = Domain.Enums.QuestionType.Text;
            string? mediaUrl = null;
            
            // Последние 4 вопроса - это IMAGE и AUDIO
            int totalQuestions = questionsWithTags.Length;
            if (questionIndex == totalQuestions - 4) // Париж IMAGE
            {
                questionType = Domain.Enums.QuestionType.Image;
                mediaUrl = "/uploads/seed/images/paris.svg"; // SVG placeholder
                _logger.LogInformation("🖼️ Creating IMAGE question: {Text}", q.Text);
            }
            else if (questionIndex == totalQuestions - 3) // Эйфелева башня IMAGE
            {
                questionType = Domain.Enums.QuestionType.Image;
                mediaUrl = "/uploads/seed/images/eiffel-tower.svg"; // SVG placeholder
                _logger.LogInformation("🖼️ Creating IMAGE question: {Text}", q.Text);
            }
            else if (questionIndex == totalQuestions - 2) // Фортепиано AUDIO
            {
                questionType = Domain.Enums.QuestionType.Audio;
                mediaUrl = "/uploads/seed/audio/piano-melody.html"; // HTML placeholder (заменить на реальный MP3)
                _logger.LogInformation("🎵 Creating AUDIO question: {Text}", q.Text);
            }
            else if (questionIndex == totalQuestions - 1) // Гитара AUDIO
            {
                questionType = Domain.Enums.QuestionType.Audio;
                mediaUrl = "/uploads/seed/audio/guitar-riff.html"; // HTML placeholder (заменить на реальный MP3)
                _logger.LogInformation("🎵 Creating AUDIO question: {Text}", q.Text);
            }
            
            var question = new Question(
                questionType,
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
            
            // Добавляем MediaAsset если есть URL
            if (!string.IsNullOrWhiteSpace(mediaUrl))
            {
                string mediaType = questionType switch
                {
                    Domain.Enums.QuestionType.Image => "IMAGE",
                    Domain.Enums.QuestionType.Audio => "AUDIO",
                    _ => "OTHER"
                };
                
                question.AddMediaAsset(mediaType, mediaUrl, "FileSystem");
                _logger.LogInformation("   Added {MediaType} asset: {Url}", mediaType, mediaUrl);
            }

            // Отправляем на ревью и одобряем
            question.SubmitForReview();
            question.Approve();

            questionsList.Add(question);
            questionIndex++;
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
            
        // 🔥 НОВОЕ: Добавляем вопросы с разными настройками fuzzy matching
        await SeedFuzzyMatchingExamplesAsync(tags);
    }
    
    /// <summary>
    /// Seed вопросов для демонстрации различных настроек fuzzy matching
    /// </summary>
    private async Task SeedFuzzyMatchingExamplesAsync(List<Tag> tags)
    {
        _logger.LogInformation("🎯 Seeding fuzzy matching example questions...");
        
        var historyTag = tags.FirstOrDefault(t => t.Name == "История");
        var scienceTag = tags.FirstOrDefault(t => t.Name == "Наука");
        var geographyTag = tags.FirstOrDefault(t => t.Name == "География");
        var generalTag = tags.FirstOrDefault(t => t.Name == "Общие знания");
        var literatureTag = tags.FirstOrDefault(t => t.Name == "Литература");
        
        // ==========================================
        // 1. ВОПРОС С ЧИСЛОВЫМ ОТВЕТОМ (STRICT)
        // ==========================================
        var q1 = new Question(
            Domain.Enums.QuestionType.Text,
            "В каком году началась Вторая мировая война?",
            Domain.Enums.Difficulty.Easy,
            "ru",
            "Год начала ВМВ");
            
        // Числовой ответ - СТРОГОЕ сравнение (только точное совпадение)
        var a1 = q1.AddAnswer(
            "1939",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: false,    // ✅ ВЫКЛЮЧЕН fuzzy matching
            maxEditDistance: 0,        // ✅ Только точное совпадение
            minConfidence: 1.0m);      // ✅ 100% уверенность
            
        // Текстовая альтернатива - мягкое сравнение
        var a2 = q1.AddAnswer(
            "тысяча девятьсот тридцать девятый",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,     // ✅ ВКЛЮЧЕН fuzzy matching
            maxEditDistance: 3,        // ✅ Допустимы опечатки в длинной фразе
            minConfidence: 0.70m);     // ✅ 70% уверенность
            
        q1.SubmitForReview();
        q1.Approve();
        if (historyTag != null) q1.AddTag(historyTag);
        
        await _context.Questions.AddAsync(q1);
        _logger.LogInformation("   ✅ Added: Year WW2 question (strict number + flexible text)");
        
        // ==========================================
        // 2. ВОПРОС С ДАТОЙ (STRICT)
        // ==========================================
        var q2 = new Question(
            Domain.Enums.QuestionType.Text,
            "Какого числа отмечается День программиста в России?",
            Domain.Enums.Difficulty.Medium,
            "ru",
            "День программиста");
            
        // Дата в формате DD.MM - строгое сравнение
        q2.AddAnswer(
            "13.09",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: false,
            maxEditDistance: 0,
            minConfidence: 1.0m);
            
        // Альтернативный формат
        q2.AddAnswer(
            "13 сентября",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 2,
            minConfidence: 0.80m);
            
        q2.SubmitForReview();
        q2.Approve();
        if (generalTag != null) q2.AddTag(generalTag);
        
        await _context.Questions.AddAsync(q2);
        _logger.LogInformation("   ✅ Added: Programmer Day question (strict date)");
        
        // ==========================================
        // 3. ВОПРОС С ТЕКСТОВЫМ ОТВЕТОМ (FLEXIBLE)
        // ==========================================
        var q3 = new Question(
            Domain.Enums.QuestionType.Text,
            "Столица Франции?",
            Domain.Enums.Difficulty.Easy,
            "ru",
            "Столица Франции");
            
        // Текстовый ответ - мягкое сравнение (допустимы опечатки)
        q3.AddAnswer(
            "Париж",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: true,     // ✅ ВКЛЮЧЕН fuzzy matching
            maxEditDistance: 2,        // ✅ Допустимы 2 опечатки
            minConfidence: 0.75m);     // ✅ 75% уверенность
            
        // Английский вариант - строгое сравнение
        q3.AddAnswer(
            "Paris",
            isPrimary: false,
            languageCode: "en",
            allowFuzzyMatch: true,
            maxEditDistance: 1,        // ✅ Только 1 опечатка для короткого слова
            minConfidence: 0.85m);
            
        q3.SubmitForReview();
        q3.Approve();
        if (geographyTag != null) q3.AddTag(geographyTag);
        
        await _context.Questions.AddAsync(q3);
        _logger.LogInformation("   ✅ Added: Paris question (flexible text)");
        
        // ==========================================
        // 4. СМЕШАННЫЙ ВОПРОС (MIXED: число + текст)
        // ==========================================
        var q4 = new Question(
            Domain.Enums.QuestionType.Text,
            "Сколько планет в Солнечной системе?",
            Domain.Enums.Difficulty.Easy,
            "ru",
            "Количество планет");
            
        // Число - строгое сравнение
        q4.AddAnswer(
            "8",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: false,
            maxEditDistance: 0,
            minConfidence: 1.0m);
            
        // Текстовый вариант - мягкое сравнение
        q4.AddAnswer(
            "восемь",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 1,
            minConfidence: 0.80m);
            
        q4.SubmitForReview();
        q4.Approve();
        if (scienceTag != null) q4.AddTag(scienceTag);
        
        await _context.Questions.AddAsync(q4);
        _logger.LogInformation("   ✅ Added: Planets question (mixed: strict number + flexible text)");
        
        // ==========================================
        // 5. НАУЧНЫЙ ТЕРМИН (MODERATE)
        // ==========================================
        var q5 = new Question(
            Domain.Enums.QuestionType.Text,
            "Как называется процесс преобразования световой энергии в химическую в растениях?",
            Domain.Enums.Difficulty.Hard,
            "ru",
            "Процесс в растениях");
            
        // Научный термин - умеренное сравнение
        q5.AddAnswer(
            "фотосинтез",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 1,        // ✅ Только 1 опечатка для термина
            minConfidence: 0.90m);     // ✅ 90% уверенность (высокая)
            
        q5.SubmitForReview();
        q5.Approve();
        if (scienceTag != null) q5.AddTag(scienceTag);
        
        await _context.Questions.AddAsync(q5);
        _logger.LogInformation("   ✅ Added: Photosynthesis question (moderate term)");
        
        // ==========================================
        // 6. ИМЯ СОБСТВЕННОЕ (MODERATE)
        // ==========================================
        var q6 = new Question(
            Domain.Enums.QuestionType.Text,
            "Кто написал роман 'Война и мир'?",
            Domain.Enums.Difficulty.Easy,
            "ru",
            "Автор 'Война и мир'");
            
        // Имя собственное - умеренное сравнение
        q6.AddAnswer(
            "Лев Толстой",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 2,        // ✅ Допустимы опечатки в имени
            minConfidence: 0.80m);
            
        // Полное имя
        q6.AddAnswer(
            "Лев Николаевич Толстой",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 3,
            minConfidence: 0.75m);
            
        // Фамилия только
        q6.AddAnswer(
            "Толстой",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 1,
            minConfidence: 0.85m);
            
        q6.SubmitForReview();
        q6.Approve();
        if (literatureTag != null) q6.AddTag(literatureTag);
        
        await _context.Questions.AddAsync(q6);
        _logger.LogInformation("   ✅ Added: Tolstoy question (moderate name with multiple answers)");
        
        // ==========================================
        // 7. КОРОТКОЕ СЛОВО (STRICT-MODERATE)
        // ==========================================
        var q7 = new Question(
            Domain.Enums.QuestionType.Text,
            "Назовите химический символ золота",
            Domain.Enums.Difficulty.Medium,
            "ru",
            "Символ золота");
            
        // Короткое слово - только 1 опечатка
        q7.AddAnswer(
            "Au",
            isPrimary: true,
            languageCode: "en",
            allowFuzzyMatch: true,
            maxEditDistance: 1,        // ✅ Только 1 опечатка для 2-буквенного слова
            minConfidence: 0.90m);     // ✅ Высокая уверенность
            
        // Полное название
        q7.AddAnswer(
            "золото",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 1,
            minConfidence: 0.85m);
            
        q7.SubmitForReview();
        q7.Approve();
        if (scienceTag != null) q7.AddTag(scienceTag);
        
        await _context.Questions.AddAsync(q7);
        _logger.LogInformation("   ✅ Added: Gold symbol question (short word)");
        
        // ==========================================
        // 8. ДЛИННАЯ ФРАЗА (VERY FLEXIBLE)
        // ==========================================
        var q8 = new Question(
            Domain.Enums.QuestionType.Text,
            "Назовите знаменитую достопримечательность Парижа в виде башни",
            Domain.Enums.Difficulty.Easy,
            "ru",
            "Башня в Париже");
            
        // Длинная фраза - очень мягкое сравнение
        q8.AddAnswer(
            "Эйфелева башня",
            isPrimary: true,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 3,        // ✅ Допустимы 3 опечатки в длинной фразе
            minConfidence: 0.70m);     // ✅ 70% уверенность
            
        // Короткий вариант
        q8.AddAnswer(
            "Эйфелевая",
            isPrimary: false,
            languageCode: "ru",
            allowFuzzyMatch: true,
            maxEditDistance: 2,
            minConfidence: 0.75m);
            
        q8.SubmitForReview();
        q8.Approve();
        if (geographyTag != null) q8.AddTag(geographyTag);
        
        await _context.Questions.AddAsync(q8);
        _logger.LogInformation("   ✅ Added: Eiffel Tower question (long phrase)");
        
        _logger.LogInformation("✅ Seeded 8 fuzzy matching example questions with various settings!");
        _logger.LogInformation("📊 Settings breakdown:");
        _logger.LogInformation("   - STRICT (numbers, dates): AllowFuzzy=false, MaxDist=0, MinConf=1.0");
        _logger.LogInformation("   - MODERATE (terms, names, short words): AllowFuzzy=true, MaxDist=1-2, MinConf=0.80-0.90");
        _logger.LogInformation("   - FLEXIBLE (text, long phrases): AllowFuzzy=true, MaxDist=2-3, MinConf=0.70-0.75");
    }
}
