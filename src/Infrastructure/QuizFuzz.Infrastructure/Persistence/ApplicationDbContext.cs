using Microsoft.EntityFrameworkCore;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence;

/// <summary>
/// Контекст базы данных приложения
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Users and Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();

    // Questions and Content
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();
    public DbSet<FuzzyAlias> FuzzyAliases => Set<FuzzyAlias>();
    public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();
    public DbSet<Hint> Hints => Set<Hint>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    // Rooms and Game
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomTagSelection> RoomTagSelections => Set<RoomTagSelection>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<PlayerInRoom> PlayersInRoom => Set<PlayerInRoom>();
    public DbSet<PlayerAnswer> PlayerAnswers => Set<PlayerAnswer>();
    public DbSet<AnswerEvaluation> AnswerEvaluations => Set<AnswerEvaluation>();
    public DbSet<Scoreboard> Scoreboards => Set<Scoreboard>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
