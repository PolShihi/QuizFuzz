using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Moderation.Commands.ApproveQuestion;
using QuizFuzz.Application.Moderation.Commands.RejectQuestion;
using QuizFuzz.Application.Moderation.Commands.SubmitQuestionSuggestion;
using QuizFuzz.Application.Moderation.Queries.GetModerationQueue;
using QuizFuzz.Application.Moderation.Queries.GetQuestionForModeration;
using QuizFuzz.Application.Moderation.Queries.GetModeratorStats;
using QuizFuzz.Domain.Enums;
using System.Security.Claims;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для модерации вопросов
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModerationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(
        IMediator mediator,
        ILogger<ModerationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Получить очередь модерации
    /// </summary>
    [HttpGet("queue")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(GetModerationQueueResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] string? status = null,
        [FromQuery] int? limit = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        _logger.LogInformation(
            "Moderation queue requested. UserId: {UserId}, Role: {Role}, Status: {Status}, Limit: {Limit}",
            userId, userRole, status ?? "All", limit ?? 100);

        QuestionStatus? questionStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<QuestionStatus>(status, true, out var parsedStatus))
        {
            questionStatus = parsedStatus;
        }

        var query = new GetModerationQueueQuery
        {
            Status = questionStatus,
            Limit = limit
        };

        var result = await _mediator.Send(query);

        if (result.Success)
        {
            _logger.LogInformation(
                "Moderation queue retrieved successfully. UserId: {UserId}, ItemsCount: {Count}",
                userId, result.Items.Count);
        }
        else
        {
            _logger.LogWarning(
                "Failed to retrieve moderation queue. UserId: {UserId}, Message: {Message}",
                userId, result.Message);
        }

        return Ok(result);
    }

    /// <summary>
    /// Получить детальную информацию о вопросе для модерации
    /// </summary>
    [HttpGet("questions/{questionId}")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(GetQuestionForModerationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionForModeration(Guid questionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _logger.LogInformation(
            "Question details requested for moderation. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}",
            questionId, userId);

        var query = new GetQuestionForModerationQuery
        {
            QuestionId = questionId
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Question not found for moderation. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}",
                questionId, userId);
            return NotFound(result);
        }

        _logger.LogInformation(
            "Question details retrieved for moderation. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}",
            questionId, userId);

        return Ok(result);
    }

    /// <summary>
    /// Одобрить вопрос
    /// </summary>
    [HttpPost("approve")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(ApproveQuestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveQuestion([FromBody] ApproveQuestionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(userId, out var moderatorId))
        {
            _logger.LogError(
                "Invalid moderator ID in token. UserId: {UserId}",
                userId);
            return BadRequest("Invalid user ID");
        }

        _logger.LogInformation(
            "Question approval initiated. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, Username: {Username}",
            request.QuestionId, moderatorId, username);

        var command = new ApproveQuestionCommand
        {
            QuestionId = request.QuestionId,
            ModeratorUserId = moderatorId,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Question approved successfully. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, ActionId: {ActionId}",
                request.QuestionId, moderatorId, result.ModerationActionId);
            return Ok(result);
        }
        else
        {
            _logger.LogWarning(
                "Question approval failed. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, Message: {Message}",
                request.QuestionId, moderatorId, result.Message);
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Отклонить вопрос
    /// </summary>
    [HttpPost("reject")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(RejectQuestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectQuestion([FromBody] RejectQuestionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(userId, out var moderatorId))
        {
            _logger.LogError(
                "Invalid moderator ID in token. UserId: {UserId}",
                userId);
            return BadRequest("Invalid user ID");
        }

        _logger.LogInformation(
            "Question rejection initiated. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, Username: {Username}, Reason: {Reason}",
            request.QuestionId, moderatorId, username, request.Reason);

        var command = new RejectQuestionCommand
        {
            QuestionId = request.QuestionId,
            ModeratorUserId = moderatorId,
            Reason = request.Reason,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Question rejected successfully. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, ActionId: {ActionId}",
                request.QuestionId, moderatorId, result.ModerationActionId);
            return Ok(result);
        }
        else
        {
            _logger.LogWarning(
                "Question rejection failed. QuestionId: {QuestionId}, ModeratorId: {ModeratorId}, Message: {Message}",
                request.QuestionId, moderatorId, result.Message);
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Предложить вопрос (для обычных пользователей)
    /// </summary>
    [HttpPost("suggest")]
    [Authorize]
    [ProducesResponseType(typeof(SubmitQuestionSuggestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuggestQuestion([FromBody] SuggestQuestionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            _logger.LogError(
                "Invalid user ID in token. UserId: {UserId}",
                userId);
            return BadRequest("Invalid user ID");
        }

        _logger.LogInformation(
            "Question suggestion submitted. UserId: {UserId}, Username: {Username}, Type: {Type}, Difficulty: {Difficulty}",
            parsedUserId, username, request.Type, request.Difficulty);

        var command = new SubmitQuestionSuggestionCommand
        {
            UserId = parsedUserId,
            PromptText = request.PromptText,
            Type = request.Type,
            Difficulty = request.Difficulty,
            CorrectAnswers = request.CorrectAnswers
                .Select(a => new QuizFuzz.Application.Moderation.Commands.SubmitQuestionSuggestion.AnswerWithFuzzySettings
                {
                    Text = a.Text,
                    AllowFuzzyMatch = a.AllowFuzzyMatch,
                    MinConfidence = a.MinConfidence
                })
                .ToList(),
            TagIds = request.TagIds,
            Title = request.Title,
            Explanation = request.Explanation
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Question suggestion submitted successfully. UserId: {UserId}, QuestionId: {QuestionId}",
                parsedUserId, result.QuestionId);
            return Ok(result);
        }
        else
        {
            _logger.LogWarning(
                "Question suggestion failed. UserId: {UserId}, Message: {Message}",
                parsedUserId, result.Message);
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Получить статистику модерации для текущего модератора
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModeratorStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var moderatorId))
        {
            _logger.LogError(
                "Invalid moderator ID in token. UserId: {UserId}",
                userId);
            return BadRequest("Invalid user ID");
        }

        _logger.LogInformation(
            "Moderator stats requested. ModeratorId: {ModeratorId}",
            moderatorId);

        try
        {
            var stats = await _mediator.Send(new GetModeratorStatsQuery { ModeratorId = moderatorId });

            _logger.LogInformation(
                "Moderator stats retrieved. ModeratorId: {ModeratorId}, TotalActions: {TotalActions}",
                moderatorId, stats.TotalActions);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving moderator stats. ModeratorId: {ModeratorId}",
                moderatorId);
            return StatusCode(500, "Error retrieving stats");
        }
    }
}

// Request DTOs
public record ApproveQuestionRequest
{
    public Guid QuestionId { get; init; }
    public string? Comment { get; init; }
}

public record RejectQuestionRequest
{
    public Guid QuestionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public record SuggestQuestionRequest
{
    public string PromptText { get; init; } = string.Empty;
    public QuizFuzz.Domain.Enums.QuestionType Type { get; init; }
    public QuizFuzz.Domain.Enums.Difficulty Difficulty { get; init; }
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
