using QuizFuzz.Shared.Dtos.Moderation;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Api;

public class ModerationApiClient : IModerationApiClient
{
    private readonly HttpClient _httpClient;

    public ModerationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ModerationQueueDto>> GetModerationQueueAsync(string? status = null)
    {
        try
        {
            var url = string.IsNullOrEmpty(status) 
                ? "api/moderation/queue" 
                : $"api/moderation/queue?status={status}";
            
            var queue = await _httpClient.GetFromJsonAsync<List<ModerationQueueDto>>(url);
            return queue ?? new List<ModerationQueueDto>();
        }
        catch
        {
            return new List<ModerationQueueDto>();
        }
    }

    public async Task<ModerationQueueDto?> GetModerationItemAsync(Guid queueId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ModerationQueueDto>($"api/moderation/queue/{queueId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ModerationQuestionDetailsDto?> GetQuestionForModerationAsync(Guid questionId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ModerationQuestionDetailsDto>($"api/moderation/questions/{questionId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ApproveAsync(ModerationActionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/moderation/approve", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RejectAsync(ModerationActionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/moderation/reject", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
