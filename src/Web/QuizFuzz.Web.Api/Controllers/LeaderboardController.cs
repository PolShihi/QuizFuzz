using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с лидербордами и статистикой
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        IUnitOfWork unitOfWork,
        ILogger<LeaderboardController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Получить глобальный лидерборд
    /// </summary>
    [HttpGet("global")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGlobalLeaderboard([FromQuery] int limit = 100)
    {
        // Получаем все scoreboards и агрегируем по пользователям
        var allScoreboards = await _unitOfWork.Scoreboards.GetAllAsync();
        
        var leaderboard = allScoreboards
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                userId = g.Key,
                username = g.First().User.Username,
                totalScore = g.Sum(s => s.ScoreTotal),
                gamesPlayed = g.Count(),
                totalCorrectAnswers = g.Sum(s => s.CorrectCount),
                totalFirstCorrect = g.Sum(s => s.UniqueCorrectCount)
            })
            .OrderByDescending(x => x.totalScore)
            .ThenByDescending(x => x.totalCorrectAnswers)
            .Take(limit)
            .Select((x, index) => new
            {
                rank = index + 1,
                x.userId,
                x.username,
                x.totalScore,
                x.gamesPlayed,
                x.totalCorrectAnswers,
                x.totalFirstCorrect
            })
            .ToList();

        return Ok(new
        {
            period = "GLOBAL",
            updatedAt = DateTime.UtcNow,
            entries = leaderboard
        });
    }

    /// <summary>
    /// Получить недельный лидерборд
    /// </summary>
    [HttpGet("weekly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyLeaderboard([FromQuery] int limit = 50)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        
        var allScoreboards = await _unitOfWork.Scoreboards.GetAllAsync();
        
        var weeklyScoreboards = allScoreboards
            .Where(s => s.UpdatedAt >= weekAgo)
            .ToList();

        var leaderboard = weeklyScoreboards
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                userId = g.Key,
                username = g.First().User.Username,
                totalScore = g.Sum(s => s.ScoreTotal),
                gamesPlayed = g.Count(),
                totalCorrectAnswers = g.Sum(s => s.CorrectCount)
            })
            .OrderByDescending(x => x.totalScore)
            .Take(limit)
            .Select((x, index) => new
            {
                rank = index + 1,
                x.userId,
                x.username,
                x.totalScore,
                x.gamesPlayed,
                x.totalCorrectAnswers
            })
            .ToList();

        return Ok(new
        {
            period = "WEEKLY",
            startDate = weekAgo,
            endDate = DateTime.UtcNow,
            entries = leaderboard
        });
    }

    /// <summary>
    /// Получить топ игроков по различным критериям
    /// </summary>
    [HttpGet("top-players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopPlayers([FromQuery] string criteria = "score", [FromQuery] int limit = 10)
    {
        var allScoreboards = await _unitOfWork.Scoreboards.GetAllAsync();
        
        var grouped = allScoreboards
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                userId = g.Key,
                username = g.First().User.Username,
                totalScore = g.Sum(s => s.ScoreTotal),
                gamesPlayed = g.Count(),
                totalCorrectAnswers = g.Sum(s => s.CorrectCount),
                totalFirstCorrect = g.Sum(s => s.UniqueCorrectCount),
                avgScorePerGame = g.Average(s => s.ScoreTotal)
            });

        var ordered = criteria.ToLower() switch
        {
            "score" => grouped.OrderByDescending(x => x.totalScore),
            "games" => grouped.OrderByDescending(x => x.gamesPlayed),
            "accuracy" => grouped.OrderByDescending(x => x.totalCorrectAnswers),
            "first" => grouped.OrderByDescending(x => x.totalFirstCorrect),
            "average" => grouped.OrderByDescending(x => x.avgScorePerGame),
            _ => grouped.OrderByDescending(x => x.totalScore)
        };

        var topPlayers = ordered
            .Take(limit)
            .Select((x, index) => new
            {
                rank = index + 1,
                x.userId,
                x.username,
                x.totalScore,
                x.gamesPlayed,
                x.totalCorrectAnswers,
                x.totalFirstCorrect,
                avgScorePerGame = Math.Round(x.avgScorePerGame, 1)
            })
            .ToList();

        return Ok(new
        {
            criteria,
            limit,
            players = topPlayers
        });
    }

    /// <summary>
    /// Получить недавние игры
    /// </summary>
    [HttpGet("recent-games")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentGames([FromQuery] int limit = 20)
    {
        var sessions = await _unitOfWork.GameSessions.GetRecentAsync(limit);

        var recentGames = sessions.Select(s => new
        {
            sessionId = s.Id,
            roomId = s.RoomId,
            roomName = s.Room.Name,
            status = s.Status.ToString(),
            startedAt = s.StartedAt,
            endedAt = s.EndedAt,
            totalRoundsPlayed = s.TotalRoundsPlayed,
            playersCount = s.Players.Count(p => p.IsActive),
            winner = s.Scoreboards
                .OrderByDescending(sb => sb.ScoreTotal)
                .Select(sb => new
                {
                    userId = sb.UserId,
                    username = sb.User.Username,
                    score = sb.ScoreTotal
                })
                .FirstOrDefault()
        });

        return Ok(new
        {
            games = recentGames
        });
    }

    /// <summary>
    /// Получить статистику по категориям/тегам
    /// </summary>
    [HttpGet("tags-stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagsStats()
    {
        var tags = await _unitOfWork.Tags.GetActiveAsync();
        
        var tagStats = new List<object>();

        foreach (var tag in tags)
        {
            var questions = await _unitOfWork.Questions.GetApprovedByTagsAsync(new[] { tag.Id });
            var questionsCount = questions.Count;

            // Получаем комнаты с этим тегом
            var rooms = await _unitOfWork.Rooms.GetAllAsync();
            var roomsWithTag = rooms.Count(r => r.TagSelections.Any(ts => ts.TagId == tag.Id));

            tagStats.Add(new
            {
                tagId = tag.Id,
                tagName = tag.Name,
                questionsCount,
                roomsUsingTag = roomsWithTag,
                isActive = tag.IsActive
            });
        }

        return Ok(new
        {
            tags = tagStats.OrderByDescending(t => ((dynamic)t).questionsCount)
        });
    }
}
