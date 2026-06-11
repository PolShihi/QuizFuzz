namespace QuizFuzz.Web.Api.Services;

public interface IRoomCleanupService
{
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}
