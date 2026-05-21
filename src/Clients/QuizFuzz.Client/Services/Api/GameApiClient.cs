using QuizFuzz.Shared.Dtos.Game;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Api;

public class GameApiClient : IGameApiClient
{
    private readonly HttpClient _httpClient;

    public GameApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GameQuestionDto?> GetCurrentQuestionAsync(Guid sessionId)
    {
        try
        {
            
            var question = await _httpClient.GetFromJsonAsync<GameQuestionDto>($"api/game/sessions/{sessionId}/current-question");
            
            if (question != null)
            {
            }
            else
            {
            }
            
            return question;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<ScoreboardDto?> GetScoreboardAsync(Guid sessionId)
    {
        try
        {
            
            var scoreboard = await _httpClient.GetFromJsonAsync<ScoreboardDto>($"api/game/sessions/{sessionId}/scoreboard");
            
            if (scoreboard != null)
            {
            }
            
            return scoreboard;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> SubmitAnswerAsync(SubmitAnswerRequest request)
    {
        try
        {
            
            var response = await _httpClient.PostAsJsonAsync("api/game/submit-answer", request);
            
            if (response.IsSuccessStatusCode)
            {
            }
            else
            {
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
