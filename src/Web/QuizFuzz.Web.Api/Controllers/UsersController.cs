using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с пользователями и профилями
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Получить текущего пользователя
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.Username,
            Email = user.Email.Value,
            Roles = user.Roles.Select(r => r.ToString()),
            user.CreatedAt,
            user.LastLoginAt,
            IsBanned = user.IsBanActive(),
            BannedUntil = user.IsBanActive() ? user.BannedUntil : null
        });
    }

    /// <summary>
    /// Получить профиль пользователя по ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found");

        // Публичная информация (без email)
        return Ok(new
        {
            user.Id,
            user.Username,
            user.CreatedAt,
            IsBanned = user.IsBanActive()
        });
    }

    /// <summary>
    /// Получить статистику текущего пользователя
    /// </summary>
    [HttpGet("me/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyStats()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        return await GetUserStatsInternal(userId.Value);
    }

    /// <summary>
    /// Получить статистику пользователя по ID
    /// </summary>
    [HttpGet("{id}/stats")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserStats(Guid id)
    {
        return await GetUserStatsInternal(id);
    }

    private async Task<IActionResult> GetUserStatsInternal(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found");

        // Получаем статистику из scoreboard
        var allScoreboards = await _unitOfWork.Scoreboards.GetByUserIdAsync(id);
        
        var gamesPlayed = allScoreboards.Count;
        var totalScore = allScoreboards.Sum(s => s.ScoreTotal);
        var totalCorrect = allScoreboards.Sum(s => s.CorrectCount);
        var totalFirstCorrect = allScoreboards.Sum(s => s.UniqueCorrectCount);
        
        // Находим выигранные игры (максимальный score в сессии)
        var gamesWon = 0;
        var sessionIds = allScoreboards.Select(s => s.SessionId).Distinct();
        
        foreach (var sessionId in sessionIds)
        {
            var sessionScoreboards = await _unitOfWork.Scoreboards.GetLeaderboardAsync(sessionId, 1);
            if (sessionScoreboards.Any() && sessionScoreboards.First().UserId == id)
                gamesWon++;
        }

        // Получаем все ответы для подсчета среднего времени
        var allAnswers = await _unitOfWork.PlayerAnswers.GetByUserIdAsync(id);
        var avgAnswerTimeMs = allAnswers.Any() 
            ? (int)allAnswers.Average(a => a.AnswerTimeMs)
            : 0;

        var accuracy = gamesPlayed > 0 && totalCorrect > 0
            ? Math.Round((double)totalCorrect / allAnswers.Count * 100, 1)
            : 0.0;

        return Ok(new
        {
            userId = id,
            username = user.Username,
            email = user.Email.Value,
            gamesPlayed,
            gamesWon,
            winRate = gamesPlayed > 0 
                ? Math.Round((double)gamesWon / gamesPlayed * 100, 1) 
                : 0.0,
            totalScore,
            totalCorrectAnswers = totalCorrect,
            totalFirstCorrect,
            accuracy,
            avgAnswerTimeMs,
            questionsAnswered = allAnswers.Count
        });
    }

    /// <summary>
    /// Обновить профиль текущего пользователя
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateProfileRequest request)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound("User not found");

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
            {
                // Проверяем уникальность
                if (await _unitOfWork.Users.IsUsernameTakenAsync(request.Username))
                    return BadRequest("Username is already taken");

                user.UpdateUsername(request.Username);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("User {UserId} updated profile", userId);

            return Ok(new { message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating user profile {UserId}", userId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить историю игр пользователя
    /// </summary>
    [HttpGet("me/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var scoreboards = await _unitOfWork.Scoreboards.GetByUserIdAsync(userId.Value);
        
        var history = scoreboards
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                sessionId = s.SessionId,
                roomName = s.Session.Room.Name,
                scoreTotal = s.ScoreTotal,
                correctCount = s.CorrectCount,
                uniqueCorrectCount = s.UniqueCorrectCount,
                playedAt = s.UpdatedAt,
                sessionStatus = s.Session.Status.ToString()
            });

        return Ok(new
        {
            page,
            pageSize,
            totalCount = scoreboards.Count,
            items = history
        });
    }

    /// <summary>
    /// Получить список пользователей (для админа/поиска)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _unitOfWork.Users.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(u => 
                u.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = users.Count;
        var items = users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                Email = u.Email.Value,
                Roles = u.Roles.Select(r => r.ToString()),
                u.CreatedAt,
                u.LastLoginAt,
                IsBanned = u.IsBanActive(),
                BannedUntil = u.IsBanActive() ? u.BannedUntil : null
            });

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            items
        });
    }
}

public record UpdateProfileRequest(string? Username);
