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
        _logger.LogDebug("Starting database seeding...");

        await SeedTagsAsync();
        await SeedAdminUserAsync();
        
        //  ВАЖНО: Сохраняем Tags и Admin перед созданием вопросов
        await _context.SaveChangesAsync();
        
        await SeedQuestionsAsync();
        await ArchiveRemovedSeededVideoQuestionsAsync();

        await _context.SaveChangesAsync();

        await SeedQuestionHintsAsync();

        await _context.SaveChangesAsync();

        _logger.LogDebug("Database seeding completed successfully!");
    }

    private sealed record SeedTagSpec(string Name, string Description);

    private sealed record SeedAliasSpec(string Text, Domain.Enums.AliasKind Kind = Domain.Enums.AliasKind.Synonym);

    private sealed record SeedAnswerSpec(
        string Text,
        bool IsPrimary,
        string LanguageCode,
        bool AllowFuzzyMatch,
        int? MaxEditDistance,
        decimal? MinConfidence,
        SeedAliasSpec[] Aliases)
    {
        public static SeedAnswerSpec StrictNumber(string text, params string[] aliases) =>
            new(text, true, "ru", false, 0, 1.0m, aliases.Select(a => new SeedAliasSpec(a)).ToArray());

        public static SeedAnswerSpec StrictDate(string text, params string[] aliases) =>
            new(text, true, "ru", false, 0, 1.0m, aliases.Select(a => new SeedAliasSpec(a)).ToArray());

        public static SeedAnswerSpec Flexible(string text, bool primary = true, int maxEditDistance = 2, decimal minConfidence = 0.75m, string languageCode = "ru", params string[] aliases) =>
            new(text, primary, languageCode, true, maxEditDistance, minConfidence, aliases.Select(a => new SeedAliasSpec(a)).ToArray());

        public static SeedAnswerSpec Moderate(string text, bool primary = true, int maxEditDistance = 1, decimal minConfidence = 0.85m, string languageCode = "ru", params string[] aliases) =>
            new(text, primary, languageCode, true, maxEditDistance, minConfidence, aliases.Select(a => new SeedAliasSpec(a)).ToArray());
    }

    private sealed record SeedHintSpec(int RevealTimeSec, string Text);

    private sealed record SeedQuestionSpec(
        string Title,
        string PromptText,
        Domain.Enums.QuestionType Type,
        Domain.Enums.Difficulty Difficulty,
        string[] TagNames,
        SeedAnswerSpec[] Answers,
        SeedHintSpec[] Hints,
        string? MediaUrl = null,
        string LanguageCode = "ru");

    private static readonly SeedTagSpec[] TagSpecs =
    {
        new("Общие знания", "Вопросы на общую эрудицию"),
        new("Наука", "Вопросы по естественным и точным наукам"),
        new("История", "Исторические события и личности"),
        new("География", "Страны, города, природа"),
        new("Спорт", "Спортивные события и достижения"),
        new("Кино", "Фильмы, актеры, режиссеры"),
        new("Музыка", "Музыкальные исполнители и произведения"),
        new("Литература", "Книги, писатели, поэты"),
        new("Искусство", "Живопись, скульптура, архитектура"),
        new("Технологии", "IT, гаджеты, инновации"),
        new("Природа", "Животные, растения, экология"),
        new("Космос", "Астрономия, космонавтика"),
        new("Еда", "Кулинария, национальные кухни"),
        new("Игры", "Настольные, видеоигры, головоломки"),
        new("Мифология", "Мифы и легенды разных народов"),
        new("Математика", "Числа, формулы, геометрия и логика"),
        new("Беларусь", "История, культура и география Беларуси"),
        new("Животные", "Фауна, зоология и особенности животных"),
        new("Растения", "Ботаника, деревья и культурные растения"),
        new("Медицина", "Анатомия, здоровье и медицинские факты"),
        new("Языки", "Лингвистика, переводы и происхождение слов"),
        new("Архитектура", "Здания, стили и известные сооружения"),
        new("Автомобили", "Марки, устройство и история транспорта"),
        new("Аудио", "Вопросы, в которых используется звуковой фрагмент")
    };

    private static SeedHintSpec[] Hints(params (int sec, string text)[] hints) =>
        hints.Select(h => new SeedHintSpec(h.sec, h.text)).ToArray();

    private static readonly string[] RemovedSeededVideoQuestionPrompts =
    {
        "Посмотрите видео. Какой объект движется по экрану?",
        "Посмотрите видео. Какого цвета основная фигура?",
        "Посмотрите видео. Сколько раз объект меняет направление движения?"
    };

    private static readonly string[] RemovedSeededVideoMediaUrls =
    {
        "/uploads/seed/video/moving-ball.webm",
        "/uploads/seed/video/blue-shape.webm",
        "/uploads/seed/video/two-bounces.webm"
    };

    private static readonly SeedQuestionSpec[] QuestionSpecs =
    {
        new(
            "Самая большая планета",
            "Какая планета Солнечной системы самая большая?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Наука", "Космос" },
            new[] { SeedAnswerSpec.Flexible("Юпитер", aliases: new[] { "Jupiter", "планета Юпитер" }) },
            Hints((5, "Это газовый гигант."), (15, "Планета известна Большим красным пятном."), (25, "Название начинается на букву «Ю»."))),
        new(
            "Количество элементов таблицы Менделеева",
            "Сколько элементов в периодической таблице Менделеева? (укажите число)",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Наука" },
            new[] { SeedAnswerSpec.StrictNumber("118", "118 элементов") },
            Hints((5, "Ответ нужно ввести числом."), (15, "Число больше 100."), (25, "На 2026 год обычно указывают 118 подтверждённых элементов."))),
        new(
            "Переход воды в пар",
            "Как называется процесс превращения воды в пар?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Наука" },
            new[] { SeedAnswerSpec.Flexible("Испарение", aliases: new[] { "парообразование", "выпаривание", "evaporation" }) },
            Hints((5, "Это физический процесс перехода вещества из жидкого состояния в газообразное."), (15, "Процесс активно происходит при нагревании."), (25, "Слово начинается на «ис»."))),
        new(
            "Самая длинная река",
            "Какая самая длинная река в мире?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "География", "Природа" },
            new[] { SeedAnswerSpec.Flexible("Амазонка", aliases: new[] { "Amazon", "река Амазонка" }) },
            Hints((5, "Река находится в Южной Америке."), (15, "Она протекает через крупнейшие тропические леса планеты."), (25, "Название начинается на «А»."))),
        new(
            "Страна Эйфелевой башни",
            "В какой стране находится Эйфелева башня?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "География", "Архитектура" },
            new[] { SeedAnswerSpec.Flexible("Франция", aliases: new[] { "France", "во Франции" }) },
            Hints((5, "Это европейская страна."), (15, "Столица этой страны — Париж."), (25, "Название начинается на «Ф»."))),
        new(
            "Столица Японии",
            "Какая столица Японии?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "География" },
            new[] { SeedAnswerSpec.Flexible("Токио", maxEditDistance: 1, minConfidence: 0.85m, aliases: new[] { "Tokyo" }) },
            Hints((5, "Это один из крупнейших мегаполисов мира."), (15, "Город находится на острове Хонсю."), (25, "Название начинается на «То»."))),
        new(
            "Окончание Второй мировой войны",
            "В каком году закончилась Вторая мировая война?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "История" },
            new[] { SeedAnswerSpec.StrictNumber("1945", "1945 год") },
            Hints((5, "Нужен год из XX века."), (15, "Война закончилась после капитуляции Германии и Японии."), (25, "Ответ — 1945."))),
        new(
            "Открытие Америки",
            "Кто открыл Америку?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "История", "География" },
            new[] { SeedAnswerSpec.Flexible("Христофор Колумб", aliases: new[] { "Колумб", "Columbus", "Christopher Columbus" }) },
            Hints((5, "Это европейский мореплаватель."), (15, "Его экспедиция достигла Америки в 1492 году."), (25, "Фамилия начинается на «Ко»."))),
        new(
            "Основатель Microsoft",
            "Кто является основателем компании Microsoft?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Технологии" },
            new[] { SeedAnswerSpec.Flexible("Билл Гейтс", aliases: new[] { "Bill Gates", "Гейтс", "Уильям Гейтс", "William Gates" }) },
            Hints((5, "Это американский предприниматель."), (15, "Его имя часто связывают с Windows."), (25, "Имя и фамилия: Билл Гейтс."))),
        new(
            "Расшифровка CPU",
            "Что означает аббревиатура CPU?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Технологии" },
            new[] { SeedAnswerSpec.Flexible("Central Processing Unit", aliases: new[] { "Центральный процессор", "процессор", "ЦП" }) },
            Hints((5, "Это главный вычислительный компонент компьютера."), (15, "На русском часто говорят: центральный процессор."), (25, "На английском: Central Processing Unit."))),
        new(
            "Режиссер фильма Титаник",
            "Кто режиссер фильма 'Титаник'?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Кино" },
            new[] { SeedAnswerSpec.Flexible("Джеймс Кэмерон", aliases: new[] { "James Cameron", "Кэмерон", "Джеймс Камерон" }) },
            Hints((5, "Этот режиссёр также снял «Аватар»."), (15, "Его имя — Джеймс."), (25, "Фамилия начинается на «Кэ»."))),
        new(
            "Главный герой Матрицы",
            "Как зовут главного героя фильма 'Матрица'?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Кино" },
            new[] { SeedAnswerSpec.Moderate("Нео", aliases: new[] { "Neo", "Томас Андерсон", "Thomas Anderson", "Мистер Андерсон" }) },
            Hints((5, "У персонажа есть хакерский псевдоним."), (15, "Настоящее имя героя — Томас Андерсон."), (25, "Псевдоним состоит из трёх букв."))),
        new(
            "Игроки в футбольной команде",
            "Сколько игроков в футбольной команде на поле?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Спорт" },
            new[] { SeedAnswerSpec.StrictNumber("11", "11 игроков") },
            Hints((5, "Ответ нужно ввести числом."), (15, "В число входит вратарь."), (25, "Ответ — 11."))),
        new(
            "Первые современные Олимпийские игры",
            "В каком городе проходили первые современные Олимпийские игры?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Hard,
            new[] { "Спорт", "История" },
            new[] { SeedAnswerSpec.Flexible("Афины", aliases: new[] { "Athens", "в Афинах" }) },
            Hints((5, "Это европейская столица."), (15, "Город находится в Греции."), (25, "Название начинается на «Аф»."))),
        new(
            "Автор Войны и мира",
            "Кто написал 'Войну и мир'?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Литература" },
            new[] { SeedAnswerSpec.Flexible("Лев Толстой", aliases: new[] { "Толстой", "Leo Tolstoy", "Л.Н. Толстой", "Лев Николаевич Толстой" }) },
            Hints((5, "Это русский писатель XIX века."), (15, "Его также знают по роману «Анна Каренина»."), (25, "Фамилия — Толстой."))),
        new(
            "Первая книга о Гарри Поттере",
            "Как называется первая книга о Гарри Поттере?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Литература" },
            new[] { SeedAnswerSpec.Flexible("Гарри Поттер и философский камень", maxEditDistance: 4, minConfidence: 0.70m, aliases: new[] { "Философский камень", "Harry Potter and the Philosopher's Stone", "Harry Potter and the Sorcerer's Stone" }) },
            Hints((5, "В названии есть магический предмет."), (15, "В британской версии это Philosopher's Stone."), (25, "По-русски: «Философский камень»."))),
        new(
            "Bohemian Rhapsody",
            "Какая группа исполняет песню 'Bohemian Rhapsody'?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Музыка" },
            new[] { SeedAnswerSpec.Moderate("Queen", languageCode: "en", aliases: new[] { "Куин", "группа Queen" }) },
            Hints((5, "Это британская рок-группа."), (15, "Вокалист группы — Фредди Меркьюри."), (25, "Название состоит из пяти букв."))),
        new(
            "Струны стандартной гитары",
            "Сколько струн у стандартной гитары?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Музыка" },
            new[] { SeedAnswerSpec.StrictNumber("6", "6 струн") },
            Hints((5, "Ответ нужно ввести числом."), (15, "У классической гитары обычно столько же струн."), (25, "Ответ — 6."))),
        new(
            "Город на изображении",
            "Какой город вы видите на изображении?",
            Domain.Enums.QuestionType.Image,
            Domain.Enums.Difficulty.Medium,
            new[] { "География" },
            new[] { SeedAnswerSpec.Flexible("Париж", aliases: new[] { "Paris" }) },
            Hints((5, "Это столица европейской страны."), (15, "Город часто называют городом любви."), (25, "Название начинается на «Па».")),
            "/uploads/seed/images/paris.svg"),
        new(
            "Достопримечательность на картинке",
            "Что изображено на картинке?",
            Domain.Enums.QuestionType.Image,
            Domain.Enums.Difficulty.Easy,
            new[] { "География", "Искусство", "Архитектура" },
            new[] { SeedAnswerSpec.Flexible("Эйфелева башня", aliases: new[] { "Eiffel Tower", "башня Эйфеля", "Эйфелева" }) },
            Hints((5, "Это известная достопримечательность Франции."), (15, "Она находится в Париже."), (25, "Это башня.")),
            "/uploads/seed/images/eiffel-tower.svg"),
        new(
            "Инструмент на аудио",
            "Какой музыкальный инструмент вы слышите?",
            Domain.Enums.QuestionType.Audio,
            Domain.Enums.Difficulty.Medium,
            new[] { "Музыка", "Аудио" },
            new[] { SeedAnswerSpec.Flexible("Фортепиано", aliases: new[] { "Piano", "пианино", "рояль" }) },
            Hints((5, "Это клавишный инструмент."), (15, "Его часто называют пианино."), (25, "Название начинается на «Фор».")),
            "/uploads/seed/audio/piano-tone.wav"),
        new(
            "Гитара на аудио",
            "Прослушайте мелодию. Какой это инструмент?",
            Domain.Enums.QuestionType.Audio,
            Domain.Enums.Difficulty.Easy,
            new[] { "Музыка", "Аудио" },
            new[] { SeedAnswerSpec.Flexible("Гитара", aliases: new[] { "Guitar" }) },
            Hints((5, "Это струнный инструмент."), (15, "Инструмент часто используют в рок-музыке."), (25, "Обычно у него шесть струн.")),
            "/uploads/seed/audio/guitar-tone.wav"),
        new(
            "Дней в високосном году",
            "Сколько дней в високосном году?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Общие знания", "Математика" },
            new[] { SeedAnswerSpec.StrictNumber("366", "366 дней") },
            Hints((5, "Ответ нужно ввести числом."), (15, "На один день больше, чем в обычном году."), (25, "Ответ — 366."))),
        new(
            "Цвет после смешивания",
            "Какой цвет получается при смешивании красного и желтого?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Искусство", "Общие знания" },
            new[] { SeedAnswerSpec.Flexible("Оранжевый", aliases: new[] { "orange", "оранжевый цвет" }) },
            Hints((5, "Это тёплый цвет."), (15, "Такой цвет у апельсина."), (25, "Название начинается на «О»."))),
        new(
            "Стороны шестиугольника",
            "Сколько сторон у шестиугольника?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Математика" },
            new[] { SeedAnswerSpec.StrictNumber("6", "6 сторон") },
            Hints((5, "Ответ содержится в названии фигуры."), (15, "Можно ввести число."), (25, "Ответ — 6."))),
        new(
            "Боязнь пауков",
            "Как называется боязнь пауков?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Hard,
            new[] { "Животные", "Медицина" },
            new[] { SeedAnswerSpec.Flexible("Арахнофобия", maxEditDistance: 2, minConfidence: 0.80m, aliases: new[] { "arachnophobia" }) },
            Hints((5, "Это разновидность фобии."), (15, "Слово связано с паукообразными."), (25, "Название начинается на «арахно»."))),
        new(
            "Минуты в трех часах",
            "Сколько минут в трех часах?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Математика", "Общие знания" },
            new[] { SeedAnswerSpec.StrictNumber("180", "180 минут") },
            Hints((5, "В одном часе 60 минут."), (15, "Нужно умножить 60 на 3."), (25, "Ответ — 180."))),
        new(
            "Начало Второй мировой войны",
            "В каком году началась Вторая мировая война?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "История" },
            new[] { SeedAnswerSpec.StrictNumber("1939", "1939 год") },
            Hints((5, "Нужен год из XX века."), (15, "Это произошло после нападения Германии на Польшу."), (25, "Ответ — 1939."))),
        new(
            "День программиста",
            "Какого числа отмечается День программиста в России?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Технологии", "Общие знания" },
            new[] { SeedAnswerSpec.StrictDate("13.09"), SeedAnswerSpec.Moderate("13 сентября", primary: false, maxEditDistance: 1, minConfidence: 0.85m) },
            Hints((5, "Праздник связан с 256-м днём года."), (15, "Обычно это дата в сентябре."), (25, "Формат ответа может быть 13.09."))),
        new(
            "Столица Франции",
            "Столица Франции?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "География" },
            new[] { SeedAnswerSpec.Flexible("Париж", maxEditDistance: 1, minConfidence: 0.85m, aliases: new[] { "Paris" }) },
            Hints((5, "Это город на реке Сене."), (15, "В этом городе находится Лувр."), (25, "Название начинается на «Па»."))),
        new(
            "Количество планет",
            "Сколько планет в Солнечной системе?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Наука", "Космос" },
            new[] { SeedAnswerSpec.StrictNumber("8"), SeedAnswerSpec.Moderate("восемь", primary: false) },
            Hints((5, "Плутон сейчас считается карликовой планетой."), (15, "Ответ меньше десяти."), (25, "Ответ — 8."))),
        new(
            "Фотосинтез",
            "Как называется процесс преобразования световой энергии в химическую в растениях?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Hard,
            new[] { "Наука", "Растения" },
            new[] { SeedAnswerSpec.Moderate("фотосинтез", maxEditDistance: 1, minConfidence: 0.90m) },
            Hints((5, "Процесс происходит в растениях и водорослях."), (15, "Для него нужен свет."), (25, "Название начинается на «фото»."))),
        new(
            "Автор романа Война и мир",
            "Кто написал роман 'Война и мир'?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Литература" },
            new[] { SeedAnswerSpec.Flexible("Лев Толстой", aliases: new[] { "Толстой", "Лев Николаевич Толстой", "Leo Tolstoy" }) },
            Hints((5, "Это русский писатель XIX века."), (15, "Его также знают по роману «Анна Каренина»."), (25, "Фамилия — Толстой."))),
        new(
            "Химический символ золота",
            "Назовите химический символ золота",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Наука" },
            new[] { SeedAnswerSpec.Moderate("Au", languageCode: "en", maxEditDistance: 0, minConfidence: 1.0m, aliases: new[] { "золото", "aurum" }) },
            Hints((5, "Это двухбуквенный латинский символ."), (15, "Первая буква — A."), (25, "Символ связан с латинским словом aurum."))),
        new(
            "Башня в Париже",
            "Назовите знаменитую достопримечательность Парижа в виде башни",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "География", "Архитектура" },
            new[] { SeedAnswerSpec.Flexible("Эйфелева башня", maxEditDistance: 3, minConfidence: 0.70m, aliases: new[] { "Эйфелевая", "Eiffel Tower" }) },
            Hints((5, "Эта достопримечательность находится во Франции."), (15, "Она построена из металла."), (25, "Название начинается на «Эйф»."))),
        new(
            "Океан между Африкой и Австралией",
            "Какой океан расположен между Африкой и Австралией?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "География" },
            new[] { SeedAnswerSpec.Flexible("Индийский океан", aliases: new[] { "Индийский" }) },
            Hints((5, "Это третий по площади океан Земли."), (15, "Его название связано с Индией."), (25, "Название начинается на «Ин»."))),
        new(
            "Белорусская столица",
            "Как называется столица Беларуси?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Беларусь", "География" },
            new[] { SeedAnswerSpec.Flexible("Минск", maxEditDistance: 1, minConfidence: 0.85m, aliases: new[] { "Minsk" }) },
            Hints((5, "Это крупнейший город страны."), (15, "Город расположен на реке Свислочь."), (25, "Название начинается на «Ми»."))),
        new(
            "Первопечатник Беларуси",
            "Кто считается первым восточнославянским книгопечатником из Полоцка?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Hard,
            new[] { "Беларусь", "История", "Литература" },
            new[] { SeedAnswerSpec.Flexible("Франциск Скорина", aliases: new[] { "Скорина", "Francysk Skaryna", "Francis Skaryna" }) },
            Hints((5, "Он связан с Полоцком и Прагой."), (15, "Издавал книги Библии на понятном народу языке."), (25, "Фамилия начинается на «Ско»."))),
        new(
            "Квадрат гипотенузы",
            "Как называется теорема о связи катетов и гипотенузы прямоугольного треугольника?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Математика" },
            new[] { SeedAnswerSpec.Flexible("теорема Пифагора", aliases: new[] { "Пифагор", "Pythagorean theorem" }) },
            Hints((5, "Формула часто записывается как a² + b² = c²."), (15, "Её связывают с древнегреческим математиком."), (25, "Имя математика — Пифагор."))),
        new(
            "Корень из 144",
            "Чему равен квадратный корень из 144?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Математика" },
            new[] { SeedAnswerSpec.StrictNumber("12") },
            Hints((5, "Ответ нужно ввести числом."), (15, "12 × 12 = 144."), (25, "Ответ — 12."))),
        new(
            "Символ воды",
            "Какая химическая формула воды?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Наука" },
            new[] { SeedAnswerSpec.Moderate("H2O", languageCode: "en", maxEditDistance: 0, minConfidence: 1.0m, aliases: new[] { "H₂O", "аш два о" }) },
            Hints((5, "Формула состоит из водорода и кислорода."), (15, "Два атома водорода и один атом кислорода."), (25, "Запись похожа на H2O."))),
        new(
            "Животное с самым длинным шеей",
            "Какое животное известно самой длинной шеей?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Животные", "Природа" },
            new[] { SeedAnswerSpec.Flexible("жираф", maxEditDistance: 1, minConfidence: 0.85m, aliases: new[] { "giraffe" }) },
            Hints((5, "Это африканское животное."), (15, "Оно питается листьями высоких деревьев."), (25, "Название начинается на «жи»."))),
        new(
            "Растение из желудя",
            "Какое дерево вырастает из желудя?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Растения", "Природа" },
            new[] { SeedAnswerSpec.Flexible("дуб", maxEditDistance: 1, minConfidence: 0.85m, aliases: new[] { "oak" }) },
            Hints((5, "Это лиственное дерево."), (15, "Его плод называется желудь."), (25, "Название состоит из трёх букв."))),
        new(
            "Самый большой орган человека",
            "Какой самый большой орган человеческого тела?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Медицина", "Наука" },
            new[] { SeedAnswerSpec.Flexible("кожа", maxEditDistance: 1, minConfidence: 0.85m, aliases: new[] { "skin" }) },
            Hints((5, "Этот орган покрывает тело снаружи."), (15, "Он защищает организм от внешней среды."), (25, "Название начинается на «ко»."))),
        new(
            "Язык слова hello",
            "На каком языке слово 'hello' означает приветствие?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Easy,
            new[] { "Языки" },
            new[] { SeedAnswerSpec.Flexible("английский", aliases: new[] { "English", "на английском" }) },
            Hints((5, "Это международный язык общения."), (15, "На нём говорят в Великобритании и США."), (25, "Название начинается на «анг»."))),
        new(
            "Происхождение слова робот",
            "Из какого языка произошло слово 'робот'?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Hard,
            new[] { "Языки", "Технологии" },
            new[] { SeedAnswerSpec.Flexible("чешский", aliases: new[] { "Czech", "чешского" }) },
            Hints((5, "Слово связано с пьесой Карела Чапека."), (15, "Это славянский язык Центральной Европы."), (25, "Название начинается на «че»."))),
        new(
            "Марка с логотипом коня",
            "Какая автомобильная марка известна логотипом с гарцующим конём?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Medium,
            new[] { "Автомобили" },
            new[] { SeedAnswerSpec.Flexible("Ferrari", languageCode: "en", aliases: new[] { "Феррари" }) },
            Hints((5, "Это итальянская марка."), (15, "Её автомобили часто красного цвета."), (25, "Название начинается на «Фер»."))),
        new(
            "Архитектор Саграда Фамилия",
            "Кто является архитектором храма Саграда Фамилия в Барселоне?",
            Domain.Enums.QuestionType.Text,
            Domain.Enums.Difficulty.Hard,
            new[] { "Архитектура", "Искусство" },
            new[] { SeedAnswerSpec.Flexible("Антонио Гауди", aliases: new[] { "Гауди", "Antoni Gaudi", "Антони Гауди" }) },
            Hints((5, "Это испанский архитектор-модернист."), (15, "Он тесно связан с Барселоной."), (25, "Фамилия начинается на «Гау»."))),
        new(
            "Код на изображении",
            "Что написано на изображении рядом с логотипом QuizFuzz?",
            Domain.Enums.QuestionType.Image,
            Domain.Enums.Difficulty.Easy,
            new[] { "Технологии", "Игры" },
            new[] { SeedAnswerSpec.Moderate("SignalR", languageCode: "en", aliases: new[] { "сигналр", "Signal R" }) },
            Hints((5, "Это технология для real-time обмена."), (15, "Она используется в ASP.NET Core."), (25, "Название начинается на Signal.")),
            "/uploads/seed/images/signalr-card.svg"),
        new(
            "Фрагмент классической музыки",
            "Прослушайте аудио. Какой жанр музыки звучит?",
            Domain.Enums.QuestionType.Audio,
            Domain.Enums.Difficulty.Medium,
            new[] { "Музыка", "Аудио" },
            new[] { SeedAnswerSpec.Flexible("классическая", aliases: new[] { "классика", "classical" }) },
            Hints((5, "Это академическая традиция."), (15, "Такую музыку часто исполняют на фортепиано и скрипке."), (25, "Название начинается на «класс».")),
            "/uploads/seed/audio/classic-tone.wav"),
    };

    private async Task SeedTagsAsync()
    {
        var existingTags = await _context.Tags.ToListAsync();
        var existingByName = existingTags.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var addedCount = 0;
        var updatedCount = 0;

        foreach (var spec in TagSpecs)
        {
            if (existingByName.TryGetValue(spec.Name, out var tag))
            {
                if (string.IsNullOrWhiteSpace(tag.Description) && !string.IsNullOrWhiteSpace(spec.Description))
                {
                    tag.UpdateDescription(spec.Description);
                    updatedCount++;
                }

                if (!tag.IsActive)
                {
                    tag.Activate();
                    updatedCount++;
                }

                continue;
            }

            var newTag = new Tag(spec.Name, spec.Description);
            await _context.Tags.AddAsync(newTag);
            existingByName[spec.Name] = newTag;
            addedCount++;
        }

        _logger.LogDebug("Seeded tags. Added: {AddedCount}, updated: {UpdatedCount}, total specs: {TotalCount}", addedCount, updatedCount, TagSpecs.Length);
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogDebug("Users already exist, skipping admin creation...");
            return;
        }

        var email = Email.Create("admin@quizfuzz.com");
        var passwordHash = _passwordHasher.HashPassword("Admin123!");

        var admin = new User("admin", email, passwordHash);
        admin.AddRole(Domain.Enums.UserRole.Admin);
        admin.AddRole(Domain.Enums.UserRole.Moderator);

        await _context.Users.AddAsync(admin);
        _logger.LogDebug("Created admin user: admin@quizfuzz.com");
        _logger.LogDebug("IMPORTANT: Default admin password is 'Admin123!' - CHANGE IT IMMEDIATELY!");
    }

    private async Task SeedQuestionsAsync()
    {
        var tags = await _context.Tags.ToListAsync();
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");

        if (adminUser == null)
        {
            _logger.LogDebug("Admin user not found, cannot seed questions");
            return;
        }

        var addedCount = 0;
        var enrichedCount = 0;

        foreach (var spec in QuestionSpecs)
        {
            var wasAdded = await UpsertQuestionAsync(spec, tags, adminUser.Id);
            if (wasAdded)
            {
                addedCount++;
            }
            else
            {
                enrichedCount++;
            }
        }

        _logger.LogDebug("Seeded questions. Added: {AddedCount}, enriched existing: {EnrichedCount}, total specs: {TotalCount}", addedCount, enrichedCount, QuestionSpecs.Length);
    }

    private async Task ArchiveRemovedSeededVideoQuestionsAsync()
    {
        var removedVideoQuestions = await _context.Questions
            .Include(q => q.MediaAssets)
            .Where(q => q.Type == Domain.Enums.QuestionType.Video &&
                (RemovedSeededVideoQuestionPrompts.Contains(q.PromptText) ||
                 q.MediaAssets.Any(m => RemovedSeededVideoMediaUrls.Contains(m.Url))))
            .ToListAsync();

        var archivedCount = 0;
        foreach (var question in removedVideoQuestions)
        {
            if (question.Status == Domain.Enums.QuestionStatus.Archived)
                continue;

            question.Archive();
            archivedCount++;
        }

        if (archivedCount > 0)
        {
            _logger.LogDebug("Archived removed seeded video questions: {ArchivedCount}", archivedCount);
        }
    }

    /// <summary>
    /// Метод оставлен для совместимости с текущим workflow SeedAsync. Основное наполнение подсказок
    /// теперь выполняется в UpsertQuestionAsync, чтобы дополнять уже созданные вопросы, а не только новые.
    /// </summary>
    private Task SeedQuestionHintsAsync()
    {
        _logger.LogDebug("Hints are seeded together with questions in an idempotent upsert workflow.");
        return Task.CompletedTask;
    }

    private async Task<bool> UpsertQuestionAsync(SeedQuestionSpec spec, List<Tag> tags, Guid adminUserId)
    {
        var question = await _context.Questions
            .Include(q => q.Answers)
                .ThenInclude(a => a.Aliases)
            .Include(q => q.Hints)
            .Include(q => q.MediaAssets)
            .Include(q => q.Tags)
            .FirstOrDefaultAsync(q => q.PromptText == spec.PromptText);

        var created = false;

        if (question == null)
        {
            question = new Question(
                spec.Type,
                spec.PromptText,
                spec.Difficulty,
                spec.LanguageCode,
                spec.Title,
                adminUserId);

            await _context.Questions.AddAsync(question);
            created = true;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(question.Title) || !question.Title.Equals(spec.Title, StringComparison.Ordinal))
                question.UpdateTitle(spec.Title);

            if (question.Difficulty != spec.Difficulty)
                question.UpdateDifficulty(spec.Difficulty);
        }

        AddMissingTags(question, spec, tags);
        AddOrUpdateMedia(question, spec);
        AddOrUpdateAnswers(question, spec);
        AddMissingHints(question, spec);

        if (question.Status == Domain.Enums.QuestionStatus.Draft)
        {
            question.SubmitForReview();
            question.Approve();
        }
        else if (question.Status == Domain.Enums.QuestionStatus.UnderReview)
        {
            question.Approve();
        }

        return created;
    }

    private static void AddMissingTags(Question question, SeedQuestionSpec spec, List<Tag> tags)
    {
        foreach (var tagName in spec.TagNames)
        {
            var tag = tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag != null)
            {
                question.AddTag(tag);
            }
        }
    }

    private static void AddOrUpdateMedia(Question question, SeedQuestionSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.MediaUrl))
            return;

        var mediaType = spec.Type switch
        {
            Domain.Enums.QuestionType.Image => "IMAGE",
            Domain.Enums.QuestionType.Audio => "AUDIO",
            Domain.Enums.QuestionType.Video => "VIDEO",
            _ => "OTHER"
        };

        var existingMedia = question.MediaAssets.FirstOrDefault(m => m.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase));
        if (existingMedia == null)
        {
            question.AddMediaAsset(mediaType, spec.MediaUrl, "FileSystem");
            return;
        }

        if (!existingMedia.Url.Equals(spec.MediaUrl, StringComparison.OrdinalIgnoreCase))
        {
            existingMedia.UpdateUrl(spec.MediaUrl);
        }
    }

    private static void AddOrUpdateAnswers(Question question, SeedQuestionSpec spec)
    {
        foreach (var answerSpec in spec.Answers)
        {
            var answer = question.Answers.FirstOrDefault(a => Normalize(a.AnswerText) == Normalize(answerSpec.Text));
            if (answer == null)
            {
                answer = question.AddAnswer(
                    answerSpec.Text,
                    answerSpec.IsPrimary,
                    answerSpec.LanguageCode,
                    answerSpec.AllowFuzzyMatch,
                    answerSpec.MaxEditDistance,
                    answerSpec.MinConfidence);
            }
            else
            {
                answer.UpdateFuzzyMatchSettings(answerSpec.AllowFuzzyMatch, answerSpec.MaxEditDistance, answerSpec.MinConfidence);
                if (answerSpec.IsPrimary && !answer.IsPrimary)
                    answer.SetAsPrimary();
            }

            AddMissingAliases(answer, answerSpec);
        }
    }

    private static void AddMissingAliases(QuestionAnswer answer, SeedAnswerSpec answerSpec)
    {
        foreach (var aliasSpec in answerSpec.Aliases)
        {
            if (Normalize(aliasSpec.Text) == Normalize(answer.AnswerText))
                continue;

            if (answer.Aliases.Any(a => Normalize(a.AliasText) == Normalize(aliasSpec.Text)))
                continue;

            try
            {
                answer.AddAlias(aliasSpec.Text, aliasSpec.Kind);
            }
            catch (InvalidOperationException)
            {
                // Идемпотентный seeding: если алиас уже есть в другом регистре/форме, пропускаем.
            }
        }
    }

    private static void AddMissingHints(Question question, SeedQuestionSpec spec)
    {
        for (var i = 0; i < spec.Hints.Length; i++)
        {
            var hintSpec = spec.Hints[i];
            var order = i + 1;

            var alreadyExists = question.Hints.Any(h =>
                h.OrderIndex == order ||
                h.HintText.Equals(hintSpec.Text, StringComparison.OrdinalIgnoreCase));

            if (!alreadyExists)
            {
                question.AddHint(order, hintSpec.Text, hintSpec.RevealTimeSec);
            }
        }
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
