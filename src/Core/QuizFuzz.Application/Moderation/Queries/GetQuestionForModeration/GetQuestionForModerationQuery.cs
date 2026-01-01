using MediatR;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;

namespace QuizFuzz.Application.Moderation.Queries.GetQuestionForModeration;

/// <summary>
/// Запрос для получения детальной информации о вопросе для модерации
/// </summary>
public record GetQuestionForModerationQuery : IRequest<GetQuestionForModerationResult>
{
    public Guid QuestionId { get; init; }
}

public record GetQuestionForModerationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public QuestionModerationDetailsDto? Question { get; init; }
}

public record QuestionModerationDetailsDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PromptText { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Explanation { get; init; }
    public Guid AuthorUserId { get; init; }
    public string AuthorUsername { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<AnswerDto> Answers { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public List<ModerationHistoryItemDto> ModerationHistory { get; init; } = new();
    public MediaAssetDto? MediaAsset { get; init; }
}

public record AnswerDto
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = string.Empty;
    public bool IsCanonical { get; init; }
    public string? FuzzyMatchStrategy { get; init; }
    public decimal? FuzzyMatchThreshold { get; init; }
}

public record ModerationHistoryItemDto
{
    public Guid ActionId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public Guid ModeratorUserId { get; init; }
    public string ModeratorUsername { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public string? Reason { get; init; }
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ActionDate { get; init; }
}

public record MediaAssetDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class GetQuestionForModerationQueryHandler : IRequestHandler<GetQuestionForModerationQuery, GetQuestionForModerationResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetQuestionForModerationQueryHandler> _logger;

    public GetQuestionForModerationQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetQuestionForModerationQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetQuestionForModerationResult> Handle(
        GetQuestionForModerationQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching question details for moderation. QuestionId: {QuestionId}",
            request.QuestionId);

        try
        {
            // Получаем вопрос со всеми связанными данными
            var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(request.QuestionId, cancellationToken);
            if (question == null)
            {
                _logger.LogWarning(
                    "Question not found. QuestionId: {QuestionId}",
                    request.QuestionId);

                return new GetQuestionForModerationResult
                {
                    Success = false,
                    Message = $"Question with ID {request.QuestionId} not found"
                };
            }

            _logger.LogDebug(
                "Question found. QuestionId: {QuestionId}, Status: {Status}, Type: {Type}",
                question.Id, question.Status, question.Type);

            // Получаем автора
            var author = question.AuthorUserId.HasValue 
                ? await _unitOfWork.Users.GetByIdAsync(question.AuthorUserId.Value, cancellationToken)
                : null;
            var authorUsername = author?.Username ?? "Unknown";

            _logger.LogTrace(
                "Author resolved. AuthorId: {AuthorId}, Username: {Username}",
                question.AuthorUserId, authorUsername);

            // Получаем историю модерации
            var moderationActions = await _unitOfWork.ModerationActions.GetByQuestionIdAsync(
                question.Id, 
                cancellationToken);

            _logger.LogDebug(
                "Retrieved moderation history. QuestionId: {QuestionId}, ActionsCount: {Count}",
                question.Id, moderationActions.Count);

            // Резолвим имена модераторов
            var moderatorIds = moderationActions.Select(ma => ma.ModeratorUserId).Distinct().ToList();
            var moderators = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var moderatorDict = moderators
                .Where(u => moderatorIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Username);

            // Формируем историю модерации
            var history = moderationActions.Select(ma =>
            {
                var moderatorUsername = moderatorDict.TryGetValue(ma.ModeratorUserId, out var username)
                    ? username
                    : "Unknown";

                return new ModerationHistoryItemDto
                {
                    ActionId = ma.Id,
                    ActionType = ma.ActionType.ToString(),
                    ModeratorUserId = ma.ModeratorUserId,
                    ModeratorUsername = moderatorUsername,
                    Comment = ma.Comment,
                    Reason = ma.Reason,
                    PreviousStatus = ma.PreviousStatus.ToString(),
                    NewStatus = ma.NewStatus.ToString(),
                    ActionDate = ma.ActionDate
                };
            })
            .OrderByDescending(h => h.ActionDate)
            .ToList();

            // Формируем ответы
            var answers = question.Answers?.Select(a => new AnswerDto
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                IsCanonical = a.IsPrimary,
                FuzzyMatchStrategy = a.AllowFuzzyMatch ? "Fuzzy" : "Exact",
                FuzzyMatchThreshold = a.MinConfidence
            }).ToList() ?? new List<AnswerDto>();

            _logger.LogTrace(
                "Prepared answers. QuestionId: {QuestionId}, AnswersCount: {Count}",
                question.Id, answers.Count);

            // Медиа-ассет если есть (пока не реализовано в Question)
            MediaAssetDto? mediaAsset = null;

            // Теги
            var tags = question.Tags?.Select(qt => qt.Tag.Name).ToList() ?? new List<string>();

            var result = new QuestionModerationDetailsDto
            {
                Id = question.Id,
                Title = question.Title ?? string.Empty,
                PromptText = question.PromptText,
                Type = question.Type.ToString(),
                Difficulty = question.Difficulty.ToString(),
                Status = question.Status.ToString(),
                Explanation = null, // Explanation не реализовано в Question
                AuthorUserId = question.AuthorUserId ?? Guid.Empty,
                AuthorUsername = authorUsername,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                Answers = answers,
                Tags = tags,
                ModerationHistory = history,
                MediaAsset = mediaAsset
            };

            _logger.LogInformation(
                "Question details fetched successfully. QuestionId: {QuestionId}, HistoryCount: {HistoryCount}",
                question.Id, history.Count);

            return new GetQuestionForModerationResult
            {
                Success = true,
                Message = "Question details retrieved successfully",
                Question = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching question for moderation. QuestionId: {QuestionId}",
                request.QuestionId);

            return new GetQuestionForModerationResult
            {
                Success = false,
                Message = $"Error fetching question: {ex.Message}"
            };
        }
    }
}
