using MediatR;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using DomainQuestionType = QuizFuzz.Domain.Enums.QuestionType;
using DomainDifficulty = QuizFuzz.Domain.Enums.Difficulty;

namespace QuizFuzz.Application.Moderation.Commands.SubmitQuestionSuggestion;

/// <summary>
/// Команда для предложения вопроса пользователем
/// </summary>
public record SubmitQuestionSuggestionCommand : IRequest<SubmitQuestionSuggestionResult>
{
    public Guid UserId { get; init; }
    public string PromptText { get; init; } = string.Empty;
    public DomainQuestionType Type { get; init; }
    public DomainDifficulty Difficulty { get; init; }
    public List<AnswerWithFuzzySettings> CorrectAnswers { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
    public string? Title { get; init; }
    public string? Explanation { get; init; }
}

public record AnswerWithFuzzySettings
{
    public string Text { get; init; } = string.Empty;
    public bool AllowFuzzyMatch { get; init; } = true;
    public double MinConfidence { get; init; } = 0.7;
}

public record SubmitQuestionSuggestionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? QuestionId { get; init; }
}

public class SubmitQuestionSuggestionCommandHandler : IRequestHandler<SubmitQuestionSuggestionCommand, SubmitQuestionSuggestionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitQuestionSuggestionCommandHandler> _logger;

    public SubmitQuestionSuggestionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SubmitQuestionSuggestionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SubmitQuestionSuggestionResult> Handle(
        SubmitQuestionSuggestionCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User submitting question suggestion. UserId: {UserId}, Type: {Type}, Difficulty: {Difficulty}",
            request.UserId, request.Type, request.Difficulty);

        try
        {
            // Получаем пользователя
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "User not found for question suggestion. UserId: {UserId}",
                    request.UserId);
                return new SubmitQuestionSuggestionResult
                {
                    Success = false,
                    Message = $"User with ID {request.UserId} not found"
                };
            }

            // Проверяем, не забанен ли пользователь
            if (user.IsBanActive())
            {
                _logger.LogWarning(
                    "Banned user attempted to submit question. UserId: {UserId}, BannedUntil: {BannedUntil}",
                    request.UserId, user.BannedUntil);
                return new SubmitQuestionSuggestionResult
                {
                    Success = false,
                    Message = "Banned users cannot submit questions"
                };
            }

            // Валидация данных
            if (string.IsNullOrWhiteSpace(request.PromptText))
            {
                _logger.LogWarning(
                    "Invalid question submission: empty prompt. UserId: {UserId}",
                    request.UserId);
                return new SubmitQuestionSuggestionResult
                {
                    Success = false,
                    Message = "Question prompt cannot be empty"
                };
            }

            if (!request.CorrectAnswers.Any())
            {
                _logger.LogWarning(
                    "Invalid question submission: no correct answers. UserId: {UserId}",
                    request.UserId);
                return new SubmitQuestionSuggestionResult
                {
                    Success = false,
                    Message = "At least one correct answer is required"
                };
            }

            _logger.LogDebug(
                "Creating question entity. UserId: {UserId}, AnswersCount: {AnswersCount}, TagsCount: {TagsCount}",
                request.UserId, request.CorrectAnswers.Count, request.TagIds.Count);

            // Создаем вопрос (изначально Draft, затем SubmitForReview)
            var question = new Question(
                request.Type,
                request.PromptText,
                request.Difficulty,
                "ru",
                request.Title,
                user.Id);

            // ВАЖНО: добавляем ответы через агрегат, иначе Question.SubmitForReview() не увидит primary answers
            // (и модерация не заработает).
            foreach (var answerData in request.CorrectAnswers)
            {
                question.AddAnswer(
                    answerData.Text,
                    isPrimary: true,
                    languageCode: "ru",
                    allowFuzzyMatch: answerData.AllowFuzzyMatch,
                    maxEditDistance: null,
                    minConfidence: (decimal)answerData.MinConfidence);

                _logger.LogTrace(
                    "Added answer to question aggregate. QuestionId: {QuestionId}, Answer: {Answer}, FuzzyMatch: {FuzzyMatch}, MinConfidence: {MinConfidence}",
                    question.Id, answerData.Text, answerData.AllowFuzzyMatch, answerData.MinConfidence);
            }

            await _unitOfWork.Questions.AddAsync(question, cancellationToken);

            _logger.LogDebug(
                "Question entity created. QuestionId: {QuestionId}, Status: {Status}",
                question.Id, question.Status);

            // Добавляем теги
            if (request.TagIds.Any())
            {
                var tags = await _unitOfWork.Tags.GetAllAsync(cancellationToken);
                foreach (var tagId in request.TagIds)
                {
                    var tag = tags.FirstOrDefault(t => t.Id == tagId);
                    if (tag != null)
                    {
                        question.AddTag(tag);
                        _logger.LogTrace(
                            "Added tag to question. QuestionId: {QuestionId}, TagId: {TagId}, TagName: {TagName}",
                            question.Id, tag.Id, tag.Name);
                    }
                }
            }

            // Теперь устанавливаем статус "На рассмотрении" после добавления ответов
            question.SubmitForReview();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Question suggestion submitted successfully. QuestionId: {QuestionId}, UserId: {UserId}, Status: {Status}",
                question.Id, user.Id, question.Status);

            return new SubmitQuestionSuggestionResult
            {
                Success = true,
                Message = "Question submitted for moderation successfully",
                QuestionId = question.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error submitting question suggestion. UserId: {UserId}",
                request.UserId);

            return new SubmitQuestionSuggestionResult
            {
                Success = false,
                Message = $"Error submitting question: {ex.Message}"
            };
        }
    }
}
