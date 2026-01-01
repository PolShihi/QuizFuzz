using MediatR;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Moderation.Commands.ApproveQuestion;

/// <summary>
/// Команда для одобрения вопроса модератором
/// </summary>
public record ApproveQuestionCommand : IRequest<ApproveQuestionResult>
{
    public Guid QuestionId { get; init; }
    public Guid ModeratorUserId { get; init; }
    public string? Comment { get; init; }
}

public record ApproveQuestionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? ModerationActionId { get; init; }
}

public class ApproveQuestionCommandHandler : IRequestHandler<ApproveQuestionCommand, ApproveQuestionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveQuestionCommandHandler> _logger;

    public ApproveQuestionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ApproveQuestionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApproveQuestionResult> Handle(
        ApproveQuestionCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting question approval process. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}",
            request.QuestionId, request.ModeratorUserId);

        try
        {
            // Получаем вопрос
            var question = await _unitOfWork.Questions.GetByIdAsync(request.QuestionId, cancellationToken);
            if (question == null)
            {
                _logger.LogWarning(
                    "Question not found for approval. QuestionId: {QuestionId}",
                    request.QuestionId);
                return new ApproveQuestionResult
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
                return new ApproveQuestionResult
                {
                    Success = false,
                    Message = $"Moderator with ID {request.ModeratorUserId} not found"
                };
            }

            // Проверяем права модератора
            if (!moderator.IsModerator && !moderator.IsAdmin)
            {
                _logger.LogWarning(
                    "User is not authorized to approve questions. UserId: {UserId}, Roles: {Roles}",
                    request.ModeratorUserId, string.Join(", ", moderator.Roles));
                return new ApproveQuestionResult
                {
                    Success = false,
                    Message = "User is not authorized to approve questions"
                };
            }

            var previousStatus = question.Status;

            // Одобряем вопрос
            question.Approve();

            _logger.LogInformation(
                "Question status changed. QuestionId: {QuestionId}, PreviousStatus: {PreviousStatus}, NewStatus: {NewStatus}",
                question.Id, previousStatus, question.Status);

            // Создаем запись о действии модерации
            var moderationAction = ModerationAction.Create(
                question,
                moderator,
                ModerationActionType.Approve,
                previousStatus,
                question.Status,
                request.Comment);

            await _unitOfWork.ModerationActions.AddAsync(moderationAction, cancellationToken);

            _logger.LogDebug(
                "ModerationAction created. ActionId: {ActionId}, Type: {ActionType}",
                moderationAction.Id, moderationAction.ActionType);

            // Сохраняем изменения
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Question approved successfully. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, ActionId: {ActionId}",
                question.Id, moderator.Id, moderationAction.Id);

            return new ApproveQuestionResult
            {
                Success = true,
                Message = "Question approved successfully",
                ModerationActionId = moderationAction.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error approving question. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}",
                request.QuestionId, request.ModeratorUserId);

            return new ApproveQuestionResult
            {
                Success = false,
                Message = $"Error approving question: {ex.Message}"
            };
        }
    }
}
