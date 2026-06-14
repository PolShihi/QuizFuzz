using Microsoft.EntityFrameworkCore;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Infrastructure.Persistence;
using QuizFuzz.Shared.Dtos.Users;

namespace QuizFuzz.Web.Api.Services;

public class UserStatsService : IUserStatsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserStatsService> _logger;

    public UserStatsService(ApplicationDbContext db, ILogger<UserStatsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserStatsDto?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return null;

        var now = DateTime.UtcNow;
        var subscription = await _db.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active && s.ExpiresAt > now)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

        var scoreboards = await _db.Scoreboards
            .AsNoTracking()
            .Include(s => s.Session)
                .ThenInclude(s => s.Room)
            .Where(s => s.UserId == userId && s.Session.Status == GameSessionStatus.Finished)
            .OrderBy(s => s.Session.EndedAt ?? s.UpdatedAt)
            .ToListAsync(cancellationToken);

        var sessionIds = scoreboards.Select(s => s.SessionId).Distinct().ToList();
        var sessionScoreboards = sessionIds.Count == 0
            ? new List<Scoreboard>()
            : await _db.Scoreboards
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => sessionIds.Contains(s.SessionId))
                .ToListAsync(cancellationToken);

        var rankedBySession = sessionScoreboards
            .GroupBy(s => s.SessionId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(s => s.ScoreTotal)
                    .ThenByDescending(s => s.CorrectCount)
                    .ThenByDescending(s => s.UniqueCorrectCount)
                    .ThenBy(s => s.User.Username)
                    .Select((scoreboard, index) => new RankedScoreboard(scoreboard, index + 1, g.Count()))
                    .ToList());

        var history = scoreboards
            .OrderByDescending(s => s.Session.EndedAt ?? s.UpdatedAt)
            .Select(s => ToHistoryItem(s, rankedBySession))
            .ToList();

        var gamesPlayed = scoreboards.Count;
        var gamesWon = history.Count(h => h.Rank == 1);
        var totalScore = scoreboards.Sum(s => s.ScoreTotal);
        var bestGameScore = scoreboards.Count == 0 ? 0 : scoreboards.Max(s => s.ScoreTotal);
        var averageScore = gamesPlayed == 0 ? 0 : Math.Round((double)totalScore / gamesPlayed, 1);

        var answers = await _db.PlayerAnswers
            .AsNoTracking()
            .Include(a => a.Evaluation)
            .Include(a => a.Round)
                .ThenInclude(r => r.Question)
                    .ThenInclude(q => q.Tags)
                        .ThenInclude(qt => qt.Tag)
            .Where(a => a.UserId == userId && a.Round.Session.Status == GameSessionStatus.Finished)
            .ToListAsync(cancellationToken);

        var attempts = answers.Count;
        var correctAnswers = answers.Count(a => a.Evaluation != null && a.Evaluation.IsCorrect);
        var wrongAnswers = attempts - correctAnswers;

        var byRound = answers
            .GroupBy(a => a.RoundId)
            .Select(g => new RoundAnswerStats(
                g.Key,
                g.First().Round,
                g.OrderBy(a => a.CreatedAt).ToList(),
                g.Any(a => a.Evaluation != null && a.Evaluation.IsCorrect),
                g.Where(a => a.Evaluation != null && a.Evaluation.IsCorrect).OrderBy(a => a.CreatedAt).FirstOrDefault()))
            .ToList();

        var questionsAnswered = byRound.Count;
        var correctRounds = byRound.Count(r => r.HasCorrectAnswer);
        var accuracy = questionsAnswered == 0 ? 0 : Math.Round((double)correctRounds / questionsAnswered * 100, 1);

        var firstCorrectAnswers = scoreboards.Sum(s => s.UniqueCorrectCount);
        var firstCorrectTimes = byRound
            .Where(r => r.FirstCorrectAnswer != null)
            .Select(r => r.FirstCorrectAnswer!.AnswerTimeMs)
            .ToList();

        var averageAnswerTime = firstCorrectTimes.Count == 0 ? 0 : (int)Math.Round(firstCorrectTimes.Average());
        var bestAnswerTime = firstCorrectTimes.Count == 0 ? 0 : firstCorrectTimes.Min();

        var currentWinStreak = CalculateCurrentWinStreak(history.OrderBy(h => h.PlayedAt).ToList());
        var bestWinStreak = CalculateBestWinStreak(history.OrderBy(h => h.PlayedAt).ToList());
        var hasPerfectGame = scoreboards.Any(s => s.Session.TotalRoundsPlayed > 0 && s.CorrectCount >= s.Session.TotalRoundsPlayed);

        var tagStats = BuildTagStats(byRound);
        var favoriteTags = tagStats
            .OrderByDescending(x => x.QuestionsAnswered)
            .ThenByDescending(x => x.Accuracy)
            .Take(5)
            .ToList();
        var eligibleTagStats = tagStats
            .Where(x => x.QuestionsAnswered >= 2)
            .ToList();

        var strongTags = eligibleTagStats
            .Where(x => x.Accuracy >= 75)
            .OrderByDescending(x => x.Accuracy)
            .ThenByDescending(x => x.QuestionsAnswered)
            .ThenBy(x => x.TagName)
            .Take(3)
            .ToList();

        var strongTagIds = strongTags
            .Select(x => x.TagId)
            .ToHashSet();

        var weakTags = eligibleTagStats
            .Where(x => x.Accuracy < 60)
            .Where(x => !strongTagIds.Contains(x.TagId))
            .OrderBy(x => x.Accuracy)
            .ThenByDescending(x => x.QuestionsAnswered)
            .ThenBy(x => x.TagName)
            .Take(3)
            .ToList();

        var authorStats = await BuildAuthorStatsAsync(userId, cancellationToken);

        var snapshot = new UserStatsSnapshot(
            gamesPlayed,
            gamesWon,
            questionsAnswered,
            correctAnswers,
            firstCorrectAnswers,
            bestAnswerTime,
            bestWinStreak,
            hasPerfectGame,
            subscription != null);

        var achievements = await SyncAchievementsAsync(userId, snapshot, cancellationToken);
        var hasPremium = subscription != null;
        var visibleAchievements = hasPremium
            ? achievements
            : achievements
                .Where(a => a.IsUnlocked)
                .Take(3)
                .ToList();

        return new UserStatsDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email.Value,
            GamesPlayed = gamesPlayed,
            GamesWon = gamesWon,
            GamesLost = Math.Max(0, gamesPlayed - gamesWon),
            TotalScore = totalScore,
            AverageScorePerGame = averageScore,
            BestGameScore = bestGameScore,
            QuestionsAnswered = questionsAnswered,
            AnswerAttempts = attempts,
            CorrectAnswers = correctAnswers,
            WrongAnswers = wrongAnswers,
            FirstCorrectAnswers = firstCorrectAnswers,
            UniqueFirstCorrect = firstCorrectAnswers,
            WinRate = gamesPlayed == 0 ? 0 : Math.Round((double)gamesWon / gamesPlayed * 100, 1),
            Accuracy = accuracy,
            AverageAnswerTimeMs = averageAnswerTime,
            BestAnswerTimeMs = bestAnswerTime,
            CurrentWinStreak = currentWinStreak,
            BestWinStreak = bestWinStreak,
            LastPlayedAt = history.FirstOrDefault()?.PlayedAt,
            HasActiveSubscription = hasPremium,
            IsDetailedStatsAvailable = hasPremium,
            SubscriptionExpiresAt = subscription?.ExpiresAt,
            FavoriteTags = hasPremium ? favoriteTags : new List<UserTagStatDto>(),
            StrongTags = hasPremium ? strongTags : new List<UserTagStatDto>(),
            WeakTags = hasPremium ? weakTags : new List<UserTagStatDto>(),
            AuthorStats = hasPremium ? authorStats : new AuthorQuestionStatsDto(),
            Achievements = visibleAchievements,
            RecentGames = hasPremium ? history.Take(5).ToList() : new List<GameHistoryItemDto>()
        };
    }

    public async Task<GameHistoryResponseDto> GetGameHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var scoreboards = await _db.Scoreboards
            .AsNoTracking()
            .Include(s => s.Session)
                .ThenInclude(s => s.Room)
            .Where(s => s.UserId == userId && s.Session.Status == GameSessionStatus.Finished)
            .OrderByDescending(s => s.Session.EndedAt ?? s.UpdatedAt)
            .ToListAsync(cancellationToken);

        var sessionIds = scoreboards.Select(s => s.SessionId).Distinct().ToList();
        var sessionScoreboards = sessionIds.Count == 0
            ? new List<Scoreboard>()
            : await _db.Scoreboards
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => sessionIds.Contains(s.SessionId))
                .ToListAsync(cancellationToken);

        var rankedBySession = sessionScoreboards
            .GroupBy(s => s.SessionId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(s => s.ScoreTotal)
                    .ThenByDescending(s => s.CorrectCount)
                    .ThenByDescending(s => s.UniqueCorrectCount)
                    .ThenBy(s => s.User.Username)
                    .Select((scoreboard, index) => new RankedScoreboard(scoreboard, index + 1, g.Count()))
                    .ToList());

        var items = scoreboards
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ToHistoryItem(s, rankedBySession))
            .ToList();

        return new GameHistoryResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = scoreboards.Count,
            Items = items
        };
    }

    private async Task<List<UserAchievementDto>> SyncAchievementsAsync(Guid userId, UserStatsSnapshot snapshot, CancellationToken cancellationToken)
    {
        var existing = await _db.UserAchievements
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var existingCodes = existing.Select(a => a.AchievementCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var definition in AchievementCatalog.Definitions)
        {
            if (!existingCodes.Contains(definition.Code) && definition.Progress(snapshot) >= definition.Target)
            {
                _db.UserAchievements.Add(new UserAchievement(userId, definition.Code, now));
                existingCodes.Add(definition.Code);
                changed = true;
            }
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
            existing = await _db.UserAchievements
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        return AchievementCatalog.Definitions
            .Select(definition =>
            {
                var unlocked = existing.FirstOrDefault(a => a.AchievementCode == definition.Code);
                return new UserAchievementDto
                {
                    Code = definition.Code,
                    Icon = definition.Icon,
                    IsUnlocked = unlocked != null,
                    UnlockedAt = unlocked?.UnlockedAt,
                    Progress = Math.Clamp(definition.Progress(snapshot), 0, definition.Target),
                    Target = definition.Target
                };
            })
            .ToList();
    }

    private static GameHistoryItemDto ToHistoryItem(Scoreboard scoreboard, IReadOnlyDictionary<Guid, List<RankedScoreboard>> rankedBySession)
    {
        var ranked = rankedBySession.TryGetValue(scoreboard.SessionId, out var list)
            ? list.FirstOrDefault(r => r.Scoreboard.UserId == scoreboard.UserId)
            : null;

        var playedAt = scoreboard.Session.EndedAt ?? scoreboard.UpdatedAt;

        return new GameHistoryItemDto
        {
            SessionId = scoreboard.SessionId,
            RoomName = scoreboard.Session.Room.Name,
            ScoreTotal = scoreboard.ScoreTotal,
            CorrectCount = scoreboard.CorrectCount,
            FirstCorrectCount = scoreboard.UniqueCorrectCount,
            Rank = ranked?.Rank ?? 0,
            PlayersCount = ranked?.PlayersCount ?? 0,
            IsWinner = ranked?.Rank == 1,
            PlayedAt = playedAt,
            SessionStatus = scoreboard.Session.Status.ToString()
        };
    }

    private static List<UserTagStatDto> BuildTagStats(IEnumerable<RoundAnswerStats> rounds)
    {
        var rows = new List<(Guid TagId, string TagName, bool IsCorrect)>();

        foreach (var round in rounds)
        {
            foreach (var questionTag in round.Round.Question.Tags)
            {
                rows.Add((questionTag.TagId, questionTag.Tag.Name, round.HasCorrectAnswer));
            }
        }

        return rows
            .GroupBy(x => new { x.TagId, x.TagName })
            .Select(g =>
            {
                var answered = g.Count();
                var correct = g.Count(x => x.IsCorrect);
                return new UserTagStatDto
                {
                    TagId = g.Key.TagId,
                    TagName = g.Key.TagName,
                    QuestionsAnswered = answered,
                    CorrectAnswers = correct,
                    Accuracy = answered == 0 ? 0 : Math.Round((double)correct / answered * 100, 1)
                };
            })
            .OrderByDescending(x => x.QuestionsAnswered)
            .ThenByDescending(x => x.Accuracy)
            .ToList();
    }

    private async Task<AuthorQuestionStatsDto> BuildAuthorStatsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var questions = await _db.Questions
            .AsNoTracking()
            .Where(q => q.AuthorUserId == userId)
            .Select(q => new { q.Id, q.Status })
            .ToListAsync(cancellationToken);

        if (questions.Count == 0)
        {
            return new AuthorQuestionStatsDto();
        }

        var questionIds = questions.Select(q => q.Id).ToList();
        var total = questions.Count;
        var approved = questions.Count(q => q.Status == QuestionStatus.Approved);
        var underReview = questions.Count(q => q.Status == QuestionStatus.UnderReview);
        var rejected = questions.Count(q => q.Status == QuestionStatus.Rejected);
        var draft = questions.Count(q => q.Status == QuestionStatus.Draft);

        var playedRoundIds = await _db.GameRounds
            .AsNoTracking()
            .Where(r => questionIds.Contains(r.QuestionId) && r.Session.Status == GameSessionStatus.Finished)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var attempts = playedRoundIds.Count == 0
            ? 0
            : await _db.PlayerAnswers
                .AsNoTracking()
                .CountAsync(a => playedRoundIds.Contains(a.RoundId), cancellationToken);

        var correctAnswers = playedRoundIds.Count == 0
            ? 0
            : await _db.PlayerAnswers
                .AsNoTracking()
                .CountAsync(a => playedRoundIds.Contains(a.RoundId) && a.Evaluation != null && a.Evaluation.IsCorrect, cancellationToken);

        return new AuthorQuestionStatsDto
        {
            TotalQuestions = total,
            ApprovedQuestions = approved,
            UnderReviewQuestions = underReview,
            RejectedQuestions = rejected,
            DraftQuestions = draft,
            ApprovalRate = total == 0 ? 0 : Math.Round((double)approved / total * 100, 1),
            TimesPlayed = playedRoundIds.Count,
            AnswerAttempts = attempts,
            CorrectAnswers = correctAnswers,
            AnswerAccuracy = attempts == 0 ? 0 : Math.Round((double)correctAnswers / attempts * 100, 1)
        };
    }

    private static int CalculateCurrentWinStreak(IReadOnlyList<GameHistoryItemDto> historyAscending)
    {
        var streak = 0;
        foreach (var item in historyAscending.Reverse())
        {
            if (!item.IsWinner)
                break;
            streak++;
        }
        return streak;
    }

    private static int CalculateBestWinStreak(IReadOnlyList<GameHistoryItemDto> historyAscending)
    {
        var best = 0;
        var current = 0;

        foreach (var item in historyAscending)
        {
            if (item.IsWinner)
            {
                current++;
                best = Math.Max(best, current);
            }
            else
            {
                current = 0;
            }
        }

        return best;
    }

    private sealed record RankedScoreboard(Scoreboard Scoreboard, int Rank, int PlayersCount);

    private sealed record RoundAnswerStats(
        Guid RoundId,
        GameRound Round,
        IReadOnlyList<PlayerAnswer> Answers,
        bool HasCorrectAnswer,
        PlayerAnswer? FirstCorrectAnswer);
}
