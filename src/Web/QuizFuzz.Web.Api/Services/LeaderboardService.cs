using Microsoft.EntityFrameworkCore;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Infrastructure.Persistence;
using QuizFuzz.Shared.Dtos.Leaderboard;

namespace QuizFuzz.Web.Api.Services;

public class LeaderboardService : ILeaderboardService
{
    private static readonly string[] AllowedPeriods = ["Global", "Weekly", "Monthly"];
    private static readonly string[] AllowedMetrics = ["Score", "Wins", "Accuracy"];

    private readonly ApplicationDbContext _db;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(ApplicationDbContext db, ILogger<LeaderboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<LeaderboardResponseDto> GetLeaderboardAsync(
        string period,
        string metric,
        int limit,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        period = Normalize(period, AllowedPeriods, "Global");
        metric = Normalize(metric, AllowedMetrics, "Score");
        limit = Math.Clamp(limit, 1, 100);

        var (periodStart, periodEnd) = GetPeriodRange(period);
        var calculatedAt = DateTime.UtcNow;

        var rows = await BuildRowsAsync(period, metric, periodStart, periodEnd, calculatedAt, cancellationToken);
        await StoreRowsAsync(period, metric, rows, cancellationToken);

        var storedRows = await _db.LeaderboardEntries
            .AsNoTracking()
            .Include(e => e.User)
            .Where(e => e.Period == period && e.Metric == metric)
            .OrderBy(e => e.Rank)
            .ToListAsync(cancellationToken);

        var userIds = storedRows.Select(r => r.UserId).ToHashSet();
        var activeSubscriptions = await GetActiveSubscriptionMapAsync(userIds, cancellationToken);

        var allEntries = storedRows.Select(row => ToDto(row, activeSubscriptions)).ToList();
        var currentUserEntry = currentUserId.HasValue
            ? allEntries.FirstOrDefault(e => e.UserId == currentUserId.Value)
            : null;

        return new LeaderboardResponseDto
        {
            Period = period,
            Metric = metric,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CalculatedAt = calculatedAt,
            TotalPlayers = allEntries.Count,
            CurrentUserEntry = currentUserEntry,
            Entries = allEntries.Take(limit).ToList()
        };
    }

    private async Task<List<LeaderboardEntry>> BuildRowsAsync(
        string period,
        string metric,
        DateTime? periodStart,
        DateTime? periodEnd,
        DateTime calculatedAt,
        CancellationToken cancellationToken)
    {
        var sessionsQuery = _db.GameSessions
            .AsNoTracking()
            .Where(s => s.Status == GameSessionStatus.Finished);

        if (periodStart.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(s => (s.EndedAt ?? s.StartedAt ?? s.CreatedAt) >= periodStart.Value);
        }

        if (periodEnd.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(s => (s.EndedAt ?? s.StartedAt ?? s.CreatedAt) < periodEnd.Value);
        }

        var sessionIds = await sessionsQuery.Select(s => s.Id).ToListAsync(cancellationToken);
        if (sessionIds.Count == 0)
            return [];

        var scoreboards = await _db.Scoreboards
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => sessionIds.Contains(s.SessionId))
            .ToListAsync(cancellationToken);

        var winsByUser = scoreboards
            .GroupBy(s => s.SessionId)
            .Select(g => g
                .OrderByDescending(s => s.ScoreTotal)
                .ThenByDescending(s => s.CorrectCount)
                .ThenByDescending(s => s.UniqueCorrectCount)
                .ThenBy(s => s.User.Username)
                .First())
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var roundStats = await _db.PlayerAnswers
            .AsNoTracking()
            .Include(a => a.Evaluation)
            .Where(a => sessionIds.Contains(a.Round.SessionId))
            .GroupBy(a => new { a.UserId, a.RoundId })
            .Select(g => new
            {
                g.Key.UserId,
                HasCorrect = g.Any(a => a.Evaluation != null && a.Evaluation.IsCorrect)
            })
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                AnsweredRounds = g.Count(),
                CorrectRounds = g.Count(x => x.HasCorrect)
            })
            .ToDictionaryAsync(x => x.UserId, x => x, cancellationToken);

        var users = scoreboards
            .GroupBy(s => s.UserId)
            .Select(g =>
            {
                roundStats.TryGetValue(g.Key, out var rounds);
                var answeredRounds = rounds?.AnsweredRounds ?? 0;
                var correctRounds = rounds?.CorrectRounds ?? 0;

                return new LeaderboardUserRow(
                    UserId: g.Key,
                    Username: g.First().User.Username,
                    TotalScore: g.Sum(s => s.ScoreTotal),
                    GamesPlayed: g.Count(),
                    GamesWon: winsByUser.GetValueOrDefault(g.Key),
                    AnsweredRounds: answeredRounds,
                    CorrectRounds: correctRounds,
                    Accuracy: answeredRounds == 0 ? 0m : Math.Round((decimal)correctRounds / answeredRounds * 100m, 2));
            })
            .ToList();

        var ordered = metric switch
        {
            "Wins" => users
                .OrderByDescending(x => x.GamesWon)
                .ThenByDescending(x => x.TotalScore)
                .ThenByDescending(x => x.Accuracy)
                .ThenBy(x => x.Username),
            "Accuracy" => users
                .Where(x => x.AnsweredRounds >= 3)
                .OrderByDescending(x => x.Accuracy)
                .ThenByDescending(x => x.AnsweredRounds)
                .ThenByDescending(x => x.TotalScore)
                .ThenBy(x => x.Username),
            _ => users
                .OrderByDescending(x => x.TotalScore)
                .ThenByDescending(x => x.GamesWon)
                .ThenByDescending(x => x.Accuracy)
                .ThenBy(x => x.Username)
        };

        return ordered
            .Select((x, index) => new LeaderboardEntry(
                period,
                metric,
                periodStart,
                periodEnd,
                x.UserId,
                index + 1,
                x.TotalScore,
                x.GamesPlayed,
                x.GamesWon,
                x.AnsweredRounds,
                x.CorrectRounds,
                x.Accuracy,
                calculatedAt))
            .ToList();
    }

    private async Task StoreRowsAsync(string period, string metric, List<LeaderboardEntry> rows, CancellationToken cancellationToken)
    {
        var existing = await _db.LeaderboardEntries
            .Where(e => e.Period == period && e.Metric == metric)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _db.LeaderboardEntries.RemoveRange(existing);
        }

        if (rows.Count > 0)
        {
            await _db.LeaderboardEntries.AddRangeAsync(rows, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, DateTime>> GetActiveSubscriptionMapAsync(HashSet<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, DateTime>();

        var now = DateTime.UtcNow;
        return await _db.UserSubscriptions
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId) && s.Status == UserSubscriptionStatus.Active && s.ExpiresAt > now)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, ExpiresAt = g.Max(s => s.ExpiresAt) })
            .ToDictionaryAsync(x => x.UserId, x => x.ExpiresAt, cancellationToken);
    }

    private static LeaderboardEntryDto ToDto(LeaderboardEntry row, IReadOnlyDictionary<Guid, DateTime> activeSubscriptions)
    {
        activeSubscriptions.TryGetValue(row.UserId, out var expiresAt);

        return new LeaderboardEntryDto
        {
            Rank = row.Rank,
            UserId = row.UserId,
            Username = row.User.Username,
            TotalScore = row.TotalScore,
            GamesPlayed = row.GamesPlayed,
            GamesWon = row.GamesWon,
            AnsweredRounds = row.AnsweredRounds,
            CorrectRounds = row.CorrectRounds,
            Accuracy = (double)row.Accuracy,
            HasActiveSubscription = expiresAt != default,
            SubscriptionExpiresAt = expiresAt == default ? null : expiresAt
        };
    }

    private static string Normalize(string? value, IReadOnlyCollection<string> allowed, string fallback)
    {
        var normalized = allowed.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        return normalized ?? fallback;
    }

    private static (DateTime? Start, DateTime? End) GetPeriodRange(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "Weekly" => (StartOfWeek(now), StartOfWeek(now).AddDays(7)),
            "Monthly" => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            _ => (null, null)
        };
    }

    private static DateTime StartOfWeek(DateTime utcNow)
    {
        var diff = ((int)utcNow.DayOfWeek + 6) % 7;
        return utcNow.Date.AddDays(-diff);
    }

    private sealed record LeaderboardUserRow(
        Guid UserId,
        string Username,
        int TotalScore,
        int GamesPlayed,
        int GamesWon,
        int AnsweredRounds,
        int CorrectRounds,
        decimal Accuracy);
}
