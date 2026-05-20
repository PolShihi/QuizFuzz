using QuizFuzz.Shared.Dtos.Questions;

namespace QuizFuzz.Client.Services.Api;

/// <summary>
/// API клиент для работы с тегами
/// </summary>
public interface ITagsApiClient
{
    Task<List<TagDto>> GetActiveTagsAsync();
    Task<TagDto?> GetTagByIdAsync(Guid id);
    Task<List<TagDto>> GetAllTagsAsync();
}
