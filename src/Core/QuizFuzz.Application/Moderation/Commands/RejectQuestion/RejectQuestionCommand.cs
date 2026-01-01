using MediatR;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Moderation.Commands.RejectQuestion;

/// <summary>
/// Команда для отклонения вопроса модератором
/// </summary>
public record RejectQuestionCommand : IRequest<RejectQuestionResult>
{
    public Guid QuestionId { get; init; }
    public Guid ModeratorUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public record RejectQuestionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? ModerationActionId { get; init; }
}

public class RejectQuestionCommandHandler : IRequestHandler<RejectQuestionCommand, RejectQuestionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectQuestionCommandHandler> _logger;

    public RejectQuestionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RejectQuestionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RejectQuestionResult> Handle(
        RejectQuestionCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting question rejection process. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, Reason: {Reason}",
            request.QuestionId, request.ModeratorUserId, request.Reason);

        try
        {
            // Получаем вопрос
            var question = await _unitOfWork.Questions.GetByIdAsync(request.QuestionId, cancellationToken);
            if (question == null)
            {
                _logger.LogWarning(
                    "Question not found for rejection. QuestionId: {QuestionId}",
                    request.QuestionId);
                return new RejectQuestionResult
                {
                    Success = false,
                    Message = $"Question with ID {request.QuestionId} not found"
                };
            }

            // Получаем модератора
            var moderator = await _unitOfWork.Users.GetByIdAsync(request.ModeratorUserId, cancellationToken);
            if (moderator == null)
            {
                _logger.LogWarning(
                    "Moderator not found. ModeratorId: {ModeratorId}",
                    request.ModeratorUserId);
                return new RejectQuestionResult
                {
                    Success = false,
                    Message = $"Moderator with ID {request.ModeratorUserId} not found"
                };
            }

            // Проверяем права модератора
            if (!moderator.IsModerator && !moderator.IsAdmin)
            {
                _logger.LogWarning(
                    "User is not authorized to reject questions. UserId: {UserId}, Roles: {Roles}",
                    request.ModeratorUserId, string.Join(", ", moderator.Roles));
                return new RejectQuestionResult
                {
                    Success = false,
                    Message = "User is not authorized to reject questions"
                };
            }

            var previousStatus = question.Status;

            // Отклоняем вопрос
            question.Reject();

            _logger.LogInformation(
                "Question status changed. QuestionId: {QuestionId}, PreviousStatus: {PreviousStatus}, NewStatus: {NewStatus}",
                question.Id, previousStatus, question.Status);

            // Создаем запись о действии модерации
            var moderationAction = ModerationAction.Create(
                question,
                moderator,
                ModerationActionType.Reject,
                previousStatus,
                question.Status,
                request.Comment,
                request.Reason);

            await _unitOfWork.ModerationActions.AddAsync(moderationAction, cancellationToken);

            _logger.LogDebug(
                "ModerationAction created. ActionId: {ActionId}, Type: {ActionType}, Reason: {Reason}",
                moderationAction.Id, moderationAction.ActionType, request.Reason);

            // Сохраняем изменения
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Question rejected successfully. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, ActionId: {ActionId}",
                question.Id, moderator.Id, moderationAction.Id);

            return new RejectQuestionResult
            {
                Success = true,
                Message = "Question rejected successfully",
                ModerationActionId = moderationAction.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error rejecting question. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}",
                request.QuestionId, request.ModeratorUserId);

            return new RejectQuestionResult
            {
                Success = false,
                Message = $"Error rejecting question: {ex.Message}"
            };
        }
    }
}
