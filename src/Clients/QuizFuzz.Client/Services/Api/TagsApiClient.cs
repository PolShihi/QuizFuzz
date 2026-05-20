using System.Net.Http.Json;
using QuizFuzz.Shared.Dtos.Questions;

namespace QuizFuzz.Client.Services.Api;

/// <summary>
/// Реализация API клиента для работы с тегами
/// </summary>
public class TagsApiClient : ITagsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TagsApiClient> _logger;

    public TagsApiClient(HttpClient httpClient, ILogger<TagsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TagDto>> GetActiveTagsAsync()
    {
        try
        {
            _logger.LogInformation("🏷️ [TagsApiClient] Fetching active tags...");
            
            var response = await _httpClient.GetAsync("api/tags");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ [TagsApiClient] Failed to fetch tags. Status: {Status}", response.StatusCode);
                return new List<TagDto>();
            }

            var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
            
            _logger.LogInformation("✅ [TagsApiClient] Loaded {Count} tags", tags?.Count ?? 0);
            
            return tags ?? new List<TagDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TagsApiClient] Error fetching tags");
            return new List<TagDto>();
        }
    }

    public async Task<TagDto?> GetTagByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("🏷️ [TagsApiClient] Fetching tag {TagId}...", id);
            
            var response = await _httpClient.GetAsync($"api/tags/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ [TagsApiClient] Failed to fetch tag. Status: {Status}", response.StatusCode);
                return null;
            }

            var tag = await response.Content.ReadFromJsonAsync<TagDto>();
            
            _logger.LogInformation("✅ [TagsApiClient] Tag loaded: {Name}", tag?.Name);
            
            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TagsApiClient] Error fetching tag {TagId}", id);
            return null;
        }
    }

    public async Task<List<TagDto>> GetAllTagsAsync()
    {
        try
        {
            _logger.LogInformation("🏷️ [TagsApiClient] Fetching all tags...");
            
            var response = await _httpClient.GetAsync("api/tags");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ [TagsApiClient] Failed to fetch all tags. Status: {Status}", response.StatusCode);
                return new List<TagDto>();
            }

            var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
            
            _logger.LogInformation("✅ [TagsApiClient] Loaded {Count} tags", tags?.Count ?? 0);
            
            return tags ?? new List<TagDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TagsApiClient] Error fetching all tags");
            return new List<TagDto>();
        }
    }

    public async Task<TagDto?> SuggestTagAsync(string name, string? description)
    {
        try
        {
            _logger.LogInformation("🏷️ [TagsApiClient] Suggesting tag {TagName}...", name);

            var response = await _httpClient.PostAsJsonAsync("api/tags/suggestions", new { name, description });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ [TagsApiClient] Failed to suggest tag. Status: {Status}", response.StatusCode);
                return null;
            }

            var tag = await response.Content.ReadFromJsonAsync<TagDto>();
            _logger.LogInformation("✅ [TagsApiClient] Tag suggestion saved: {Name}", tag?.Name);
            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TagsApiClient] Error suggesting tag {TagName}", name);
            return null;
        }
    }

}
