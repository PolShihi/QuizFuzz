using QuizFuzz.Shared.Dtos.Users;

namespace QuizFuzz.Client.Services.Api;

public interface IUsersApiClient
{
    Task<UserStatsDto?> GetUserStatsAsync(Guid userId);
    Task<UserStatsDto?> GetMyStatsAsync();
}
