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
            Console.WriteLine($"🔍 [GameApiClient] GetCurrentQuestionAsync - SessionId: {sessionId}");
            
            var question = await _httpClient.GetFromJsonAsync<GameQuestionDto>($"api/game/sessions/{sessionId}/current-question");
            
            if (question != null)
            {
                Console.WriteLine($"✅ [GameApiClient] Question received:");
                Console.WriteLine($"   📝 Text: {question.Text}");
                Console.WriteLine($"   🎯 Type: {question.QuestionType}");
                Console.WriteLine($"   🔗 MediaUrl: {question.MediaUrl ?? "NULL"}");
                Console.WriteLine($"   ⏱️ TimeLimit: {question.TimeLimit}s");
            }
            else
            {
                Console.WriteLine($"⚠️ [GameApiClient] Question is NULL");
            }
            
            return question;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [GameApiClient] Error getting question: {ex.Message}");
            return null;
        }
    }

    public async Task<ScoreboardDto?> GetScoreboardAsync(Guid sessionId)
    {
        try
        {
            Console.WriteLine($"🔍 [GameApiClient] GetScoreboardAsync - SessionId: {sessionId}");
            
            var scoreboard = await _httpClient.GetFromJsonAsync<ScoreboardDto>($"api/game/sessions/{sessionId}/scoreboard");
            
            if (scoreboard != null)
            {
                Console.WriteLine($"✅ [GameApiClient] Scoreboard received with {scoreboard.Players.Count} players");
            }
            
            return scoreboard;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [GameApiClient] Error getting scoreboard: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SubmitAnswerAsync(SubmitAnswerRequest request)
    {
        try
        {
            Console.WriteLine($"🔍 [GameApiClient] SubmitAnswerAsync - Answer: {request.AnswerText}");
            
            var response = await _httpClient.PostAsJsonAsync("api/game/submit-answer", request);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ [GameApiClient] Answer submitted successfully");
            }
            else
            {
                Console.WriteLine($"❌ [GameApiClient] Answer submission failed: {response.StatusCode}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [GameApiClient] Error submitting answer: {ex.Message}");
            return false;
        }
    }
}
