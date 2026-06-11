using QuizFuzz.Shared.Dtos.Leaderboard;

namespace QuizFuzz.Client.Services.Api;

public interface ILeaderboardApiClient
{
    Task<LeaderboardResponseDto?> GetLeaderboardAsync(string period, string metric, int limit = 50);
}
