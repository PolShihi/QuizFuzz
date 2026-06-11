using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Shared.Dtos.Users;
using QuizFuzz.Web.Api.Services;

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
    private readonly IUserStatsService _userStatsService;

    public UsersController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger,
        IUserStatsService userStatsService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _userStatsService = userStatsService;
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

        var subscription = await _unitOfWork.UserSubscriptions.GetCurrentActiveByUserIdAsync(user.Id, DateTime.UtcNow);

        return Ok(new
        {
            user.Id,
            user.Username,
            Email = user.Email.Value,
            Roles = user.Roles.Select(r => r.ToString()),
            user.CreatedAt,
            user.LastLoginAt,
            IsBanned = user.IsBanActive(),
            BannedUntil = user.IsBanActive() ? user.BannedUntil : null,
            HasActiveSubscription = subscription != null,
            SubscriptionExpiresAt = subscription?.ExpiresAt
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
        var subscription = await _unitOfWork.UserSubscriptions.GetCurrentActiveByUserIdAsync(user.Id, DateTime.UtcNow);

        return Ok(new
        {
            user.Id,
            user.Username,
            user.CreatedAt,
            IsBanned = user.IsBanActive(),
            HasActiveSubscription = subscription != null,
            SubscriptionExpiresAt = subscription?.ExpiresAt
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
        var stats = await _userStatsService.GetUserStatsAsync(id);
        if (stats == null)
            return NotFound($"User with ID {id} not found");

        return Ok(stats);
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
    public async Task<IActionResult> GetGameHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var history = await _userStatsService.GetGameHistoryAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(history);
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
