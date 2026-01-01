using MediatR;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Moderation.Queries.GetModerationQueue;

/// <summary>
/// Запрос для получения очереди модерации
/// </summary>
public record GetModerationQueueQuery : IRequest<GetModerationQueueResult>
{
    public QuestionStatus? Status { get; init; }
    public int? Limit { get; init; }
}

public record GetModerationQueueResult
{
    public bool Success { get; init; }
    public List<ModerationQueueItemDto> Items { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}

public record ModerationQueueItemDto
{
    public Guid QuestionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PromptText { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid AuthorUserId { get; init; }
    public string AuthorUsername { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int AnswersCount { get; init; }
    public List<string> Tags { get; init; } = new();
    public ModerationHistoryDto? LastModerationAction { get; init; }
}

public record ModerationHistoryDto
{
    public Guid ActionId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public Guid ModeratorUserId { get; init; }
    public string ModeratorUsername { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public string? Reason { get; init; }
    public DateTime ActionDate { get; init; }
}

public class GetModerationQueueQueryHandler : IRequestHandler<GetModerationQueueQuery, GetModerationQueueResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetModerationQueueQueryHandler> _logger;

    public GetModerationQueueQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetModerationQueueQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetModerationQueueResult> Handle(
        GetModerationQueueQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching moderation queue. Status: {Status}, Limit: {Limit}",
            request.Status?.ToString() ?? "All", request.Limit ?? 100);

        try
        {
            // Получаем вопросы в зависимости от статуса
            var questions = request.Status.HasValue
                ? await _unitOfWork.Questions.GetByStatusAsync(request.Status.Value, cancellationToken)
                : await _unitOfWork.Questions.GetAllAsync(cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} questions from repository",
                questions.Count);

            // Получаем пользователей для резолва имен авторов
            var userIds = questions.Where(q => q.AuthorUserId.HasValue).Select(q => q.AuthorUserId!.Value).Distinct().ToList();
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var userDict = users.ToDictionary(u => u.Id, u => u.Username);

            _logger.LogTrace(
                "Retrieved {Count} users for username resolution",
                users.Count);

            // Получаем последние действия модерации для каждого вопроса
            var questionIds = questions.Select(q => q.Id).ToList();
            var allModerationActions = new List<Domain.Entities.ModerationAction>();
            
            foreach (var questionId in questionIds)
            {
                var actions = await _unitOfWork.ModerationActions.GetByQuestionIdAsync(questionId, cancellationToken);
                allModerationActions.AddRange(actions);
            }

            var latestActionsByQuestion = allModerationActions
                .GroupBy(ma => ma.QuestionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(ma => ma.ActionDate).First());

            _logger.LogTrace(
                "Retrieved moderation history for {Count} questions",
                latestActionsByQuestion.Count);

            // Формируем DTO
            var items = questions.Select(q =>
            {
                var authorUsername = q.AuthorUserId.HasValue && userDict.TryGetValue(q.AuthorUserId.Value, out var username) 
                    ? username 
                    : "Unknown";

                ModerationHistoryDto? lastAction = null;
                if (latestActionsByQuestion.TryGetValue(q.Id, out var action))
                {
                    var moderatorUsername = userDict.TryGetValue(action.ModeratorUserId, out var modUsername)
                        ? modUsername
                        : "Unknown";

                    lastAction = new ModerationHistoryDto
                    {
                        ActionId = action.Id,
                        ActionType = action.ActionType.ToString(),
                        ModeratorUserId = action.ModeratorUserId,
                        ModeratorUsername = moderatorUsername,
                        Comment = action.Comment,
                        Reason = action.Reason,
                        ActionDate = action.ActionDate
                    };
                }

                return new ModerationQueueItemDto
                {
                    QuestionId = q.Id,
                    Title = q.Title ?? string.Empty,
                    PromptText = q.PromptText,
                    Type = q.Type.ToString(),
                    Difficulty = q.Difficulty.ToString(),
                    Status = q.Status.ToString(),
                    AuthorUserId = q.AuthorUserId ?? Guid.Empty,
                    AuthorUsername = authorUsername,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt,
                    AnswersCount = q.Answers?.Count ?? 0,
                    Tags = q.Tags?.Select(qt => qt.Tag.Name).ToList() ?? new List<string>(),
                    LastModerationAction = lastAction
                };
            })
            .OrderByDescending(item => item.UpdatedAt)
            .ToList();

            // Применяем лимит если указан
            if (request.Limit.HasValue && request.Limit.Value > 0)
            {
                items = items.Take(request.Limit.Value).ToList();
            }

            _logger.LogInformation(
                "Moderation queue fetched successfully. Total items: {Count}",
                items.Count);

            return new GetModerationQueueResult
            {
                Success = true,
                Items = items,
                Message = $"Retrieved {items.Count} items"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching moderation queue. Status: {Status}",
                request.Status?.ToString() ?? "All");

            return new GetModerationQueueResult
            {
                Success = false,
                Items = new List<ModerationQueueItemDto>(),
                Message = $"Error fetching moderation queue: {ex.Message}"
            };
        }
    }
}
