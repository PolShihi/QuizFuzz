using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Web.Api.Services;

namespace QuizFuzz.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ILeaderboardService leaderboardService,
        ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string period = "global",
        [FromQuery] string metric = "score",
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var leaderboard = await _leaderboardService.GetLeaderboardAsync(period, metric, limit, currentUserId, cancellationToken);
        return Ok(leaderboard);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyLeaderboardRank(
        [FromQuery] string period = "global",
        [FromQuery] string metric = "score",
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var leaderboard = await _leaderboardService.GetLeaderboardAsync(period, metric, 100, currentUserId, cancellationToken);
        return Ok(leaderboard.CurrentUserEntry);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
