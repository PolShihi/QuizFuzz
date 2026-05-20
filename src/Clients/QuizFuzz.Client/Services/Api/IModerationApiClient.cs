using QuizFuzz.Shared.Dtos.Moderation;

namespace QuizFuzz.Client.Services.Api;

public interface IModerationApiClient
{
    Task<List<ModerationQueueDto>> GetModerationQueueAsync(string? status = null);
    Task<ModerationQueueDto?> GetModerationItemAsync(Guid queueId);
    Task<ModerationQuestionDetailsDto?> GetQuestionForModerationAsync(Guid questionId);
    Task<bool> ApproveAsync(ModerationActionRequest request);
    Task<bool> RejectAsync(ModerationActionRequest request);
}
