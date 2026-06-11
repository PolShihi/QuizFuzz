using QuizFuzz.Shared.Dtos.Leaderboard;

namespace QuizFuzz.Web.Api.Services;

public interface ILeaderboardService
{
    Task<LeaderboardResponseDto> GetLeaderboardAsync(
        string period,
        string metric,
        int limit,
        Guid? currentUserId,
        CancellationToken cancellationToken = default);
}
