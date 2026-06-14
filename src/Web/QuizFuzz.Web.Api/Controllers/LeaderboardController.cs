using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Infrastructure.Persistence;
using QuizFuzz.Web.Api.Services;

namespace QuizFuzz.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ILeaderboardService leaderboardService,
        ApplicationDbContext db,
        ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string period = "global",
        [FromQuery] string metric = "score",
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        if (!await HasActiveSubscriptionAsync(currentUserId.Value, cancellationToken))
            return Forbid();

        var leaderboard = await _leaderboardService.GetLeaderboardAsync(period, metric, limit, currentUserId, cancellationToken);
        return Ok(leaderboard);
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyLeaderboardRank(
        [FromQuery] string period = "global",
        [FromQuery] string metric = "score",
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        if (!await HasActiveSubscriptionAsync(currentUserId.Value, cancellationToken))
            return Forbid();

        var leaderboard = await _leaderboardService.GetLeaderboardAsync(period, metric, 100, currentUserId, cancellationToken);
        return Ok(leaderboard.CurrentUserEntry);
    }

    private async Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _db.UserSubscriptions
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active && s.ExpiresAt > now, cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
