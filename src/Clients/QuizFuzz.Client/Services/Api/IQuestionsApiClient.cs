using QuizFuzz.Shared.Dtos.Questions;

namespace QuizFuzz.Client.Services.Api;

public interface IQuestionsApiClient
{
    Task<List<QuestionDto>> GetQuestionsAsync(string? status = null, string? difficulty = null, IEnumerable<Guid>? tagIds = null);
    Task<QuestionDto?> GetQuestionAsync(Guid questionId);
    Task<QuestionDto?> CreateQuestionAsync(CreateQuestionRequest request);
    Task<QuestionDto?> UpdateQuestionAsync(Guid questionId, UpdateQuestionFullRequest request);
    Task<bool> DeleteQuestionAsync(Guid questionId);
    Task<bool> ApproveQuestionAsync(Guid questionId);
    Task<bool> RejectQuestionAsync(Guid questionId, string reason);
    Task<bool> SuggestQuestionAsync(object request);
}
