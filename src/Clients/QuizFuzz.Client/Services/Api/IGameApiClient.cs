using QuizFuzz.Shared.Dtos.Game;

namespace QuizFuzz.Client.Services.Api;

public interface IGameApiClient
{
    Task<GameQuestionDto?> GetCurrentQuestionAsync(Guid sessionId);
    Task<ScoreboardDto?> GetScoreboardAsync(Guid sessionId);
    Task<bool> SubmitAnswerAsync(SubmitAnswerRequest request);
}
