using System.Net.Http.Json;
using QuizFuzz.Shared.Dtos.Leaderboard;

namespace QuizFuzz.Client.Services.Api;

public class LeaderboardApiClient : ILeaderboardApiClient
{
    private readonly HttpClient _httpClient;

    public LeaderboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LeaderboardResponseDto?> GetLeaderboardAsync(string period, string metric, int limit = 50)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LeaderboardResponseDto>(
                $"api/leaderboard?period={Uri.EscapeDataString(period)}&metric={Uri.EscapeDataString(metric)}&limit={limit}");
        }
        catch
        {
            return null;
        }
    }
}
