using QuizFuzz.Shared.Dtos.Admin;
using QuizFuzz.Shared.Dtos.Questions;

namespace QuizFuzz.Client.Services.Api;

public interface IAdminApiClient
{
    Task<List<UserManagementDto>> GetUsersAsync();
    Task<UserManagementDto?> GetUserAsync(Guid userId);
    Task<bool> UpdateUserRolesAsync(UpdateUserRolesRequest request);
    Task<bool> BanUserAsync(BanUserRequest request);
    Task<bool> UnbanUserAsync(Guid userId);
    
    Task<List<TagDto>> GetTagsAsync();
    Task<TagDto?> CreateTagAsync(string name, string? description);
    Task<TagDto?> UpdateTagDescriptionAsync(Guid tagId, string? description);
    Task<bool> ActivateTagAsync(Guid tagId);
    Task<bool> DeleteTagAsync(Guid tagId);
}
