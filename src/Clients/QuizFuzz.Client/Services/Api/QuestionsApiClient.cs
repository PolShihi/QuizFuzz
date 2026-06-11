using QuizFuzz.Shared.Dtos.Questions;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Api;

public class QuestionsApiClient : IQuestionsApiClient
{
    private readonly HttpClient _httpClient;

    public QuestionsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<QuestionDto>> GetQuestionsAsync(string? status = null, string? difficulty = null, IEnumerable<Guid>? tagIds = null)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(status)) query.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(difficulty)) query.Add($"difficulty={Uri.EscapeDataString(difficulty)}");
            if (tagIds != null)
            {
                foreach (var tagId in tagIds.Where(id => id != Guid.Empty).Distinct())
                {
                    query.Add($"tagIds={tagId}");
                }
            }
            
            var queryString = query.Any() ? "?" + string.Join("&", query) : "";
            var questions = await _httpClient.GetFromJsonAsync<List<QuestionDto>>($"api/questions{queryString}");
            return questions ?? new List<QuestionDto>();
        }
        catch
        {
            return new List<QuestionDto>();
        }
    }

    public async Task<QuestionDto?> GetQuestionAsync(Guid questionId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<QuestionDto>($"api/questions/{questionId}/full");
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuestionDto?> CreateQuestionAsync(CreateQuestionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/questions", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<QuestionDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuestionDto?> UpdateQuestionAsync(Guid questionId, UpdateQuestionFullRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/questions/{questionId}/full", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<QuestionDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/questions/{questionId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ApproveQuestionAsync(Guid questionId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/questions/{questionId}/approve", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RejectQuestionAsync(Guid questionId, string reason)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/questions/{questionId}/reject", new { reason });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SuggestQuestionAsync(object request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/moderation/suggest", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
