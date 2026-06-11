using QuizFuzz.Shared.Dtos.Users;

namespace QuizFuzz.Web.Api.Services;

public interface IUserStatsService
{
    Task<UserStatsDto?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GameHistoryResponseDto> GetGameHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
