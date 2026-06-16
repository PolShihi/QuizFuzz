using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Moderation.Commands.ApproveQuestion;
using QuizFuzz.Application.Moderation.Commands.RejectQuestion;
using QuizFuzz.Application.Moderation.Commands.SubmitQuestionSuggestion;
using QuizFuzz.Application.Moderation.Queries.GetModeratorStats;
using QuizFuzz.Application.Moderation.Queries.GetQuestionForModeration;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Moderation;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<ModerationController> logger)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Получить очередь модерации (совместимость с Blazor клиентом)
    /// </summary>
    [HttpGet("queue")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(List<ModerationQueueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] string? status = null,
        [FromQuery] int? limit = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        _logger.LogDebug(
            "Moderation queue requested. UserId: {UserId}, Role: {Role}, Status: {Status}, Limit: {Limit}",
            userId, userRole, status ?? "All", limit ?? 100);

        QuestionStatus? questionStatus = null;
        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<QuestionStatus>(status, true, out var parsedStatus))
        {
            questionStatus = parsedStatus;
        }

        var take = limit.GetValueOrDefault(100);
        if (take <= 0)
        {
            take = 100;
        }

        var questions = await _unitOfWork.Questions.GetForModerationQueueAsync(
            questionStatus,
            take,
            HttpContext.RequestAborted);

        var items = new List<ModerationQueueDto>(questions.Count);
        foreach (var question in questions)
        {
            var latestAction = await _unitOfWork.ModerationActions.GetLatestByQuestionIdAsync(
                question.Id,
                HttpContext.RequestAborted);

            string? moderatorUsername = null;
            if (latestAction != null)
            {
                var moderator = await _unitOfWork.Users.GetByIdAsync(
                    latestAction.ModeratorUserId,
                    HttpContext.RequestAborted);
                moderatorUsername = moderator?.Username;
            }

            items.Add(new ModerationQueueDto
            {
                Id = question.Id, // QueueId == QuestionId
                QuestionId = question.Id,
                QuestionTitle = question.Title ?? string.Empty,
                QuestionText = question.PromptText,
                Difficulty = question.Difficulty.ToString(),
                Tags = question.Tags?.Select(qt => qt.Tag.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList() ?? new List<string>(),
                Status = ToClientStatus(question.Status),
                SubmittedByUserId = question.AuthorUserId ?? Guid.Empty,
                SubmittedByUsername = question.Author?.Username ?? "Unknown",
                ModeratorUserId = latestAction?.ModeratorUserId,
                ModeratorUsername = moderatorUsername,
                Comment = latestAction?.Comment,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt
            });
        }

        return Ok(items);
    }

    /// <summary>
    /// Получить один элемент очереди (совместимость с клиентом)
    /// </summary>
    [HttpGet("queue/{queueId:guid}")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(ModerationQueueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModerationQueueItem(Guid queueId)
    {
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(queueId, HttpContext.RequestAborted);
        if (question == null)
        {
            return NotFound();
        }

        var latestAction = await _unitOfWork.ModerationActions.GetLatestByQuestionIdAsync(
            question.Id,
            HttpContext.RequestAborted);

        string? moderatorUsername = null;
        if (latestAction != null)
        {
            var moderator = await _unitOfWork.Users.GetByIdAsync(latestAction.ModeratorUserId, HttpContext.RequestAborted);
            moderatorUsername = moderator?.Username;
        }

        return Ok(new ModerationQueueDto
        {
            Id = question.Id,
            QuestionId = question.Id,
            QuestionTitle = question.Title ?? string.Empty,
            QuestionText = question.PromptText,
            Difficulty = question.Difficulty.ToString(),
            Tags = question.Tags?.Select(qt => qt.Tag.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList() ?? new List<string>(),
            Status = ToClientStatus(question.Status),
            SubmittedByUserId = question.AuthorUserId ?? Guid.Empty,
            SubmittedByUsername = question.Author?.Username ?? "Unknown",
            ModeratorUserId = latestAction?.ModeratorUserId,
            ModeratorUsername = moderatorUsername,
            Comment = latestAction?.Comment,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        });
    }

    /// <summary>
    /// Одобрить вопрос (QueueId == QuestionId)
    /// </summary>
    [HttpPost("approve")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(ApproveQuestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveQuestion([FromBody] ModerationActionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var moderatorId))
        {
            return BadRequest("Invalid user ID");
        }

        var command = new ApproveQuestionCommand
        {
            QuestionId = request.QueueId,
            ModeratorUserId = moderatorId,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Отклонить вопрос (QueueId == QuestionId)
    /// </summary>
    [HttpPost("reject")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(RejectQuestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectQuestion([FromBody] ModerationActionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var moderatorId))
        {
            return BadRequest("Invalid user ID");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Rejected by moderator" : request.Reason;
        var command = new RejectQuestionCommand
        {
            QuestionId = request.QueueId,
            ModeratorUserId = moderatorId,
            Reason = reason,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
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
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return BadRequest("Invalid user ID");
        }

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
            MediaUrl = request.MediaUrl,
            Title = request.Title,
            Explanation = request.Explanation,
            Hints = (request.Hints ?? new List<QuizFuzz.Shared.Dtos.Questions.CreateHintRequest>())
                .Where(h => !string.IsNullOrWhiteSpace(h.HintText))
                .Select((h, index) => new HintSuggestionData
                {
                    OrderIndex = index,
                    HintText = h.HintText.Trim(),
                    RevealTimeSeconds = h.RevealTimeSeconds
                })
                .ToList()
        };

        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Получить детальную информацию о вопросе для модерации (для UI Review)
    /// </summary>
    [HttpGet("questions/{questionId:guid}")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(ModerationQuestionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionForModeration(Guid questionId)
    {
        var result = await _mediator.Send(new GetQuestionForModerationQuery { QuestionId = questionId });
        if (!result.Success || result.Question == null)
        {
            return NotFound(result.Message);
        }

        var dto = new ModerationQuestionDetailsDto
        {
            Id = result.Question.Id,
            Title = result.Question.Title,
            PromptText = result.Question.PromptText,
            Type = result.Question.Type,
            Difficulty = result.Question.Difficulty,
            Status = result.Question.Status,
            AuthorUserId = result.Question.AuthorUserId,
            AuthorUsername = result.Question.AuthorUsername,
            CreatedAt = result.Question.CreatedAt,
            UpdatedAt = result.Question.UpdatedAt,
            Tags = result.Question.Tags.ToList(),
            ModerationHistory = result.Question.ModerationHistory.Select(h => new QuizFuzz.Shared.Dtos.Moderation.ModerationHistoryItemDto
            {
                ActionId = h.ActionId,
                ActionType = h.ActionType,
                ModeratorUserId = h.ModeratorUserId,
                ModeratorUsername = h.ModeratorUsername,
                Comment = h.Comment,
                Reason = h.Reason,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                ActionDate = h.ActionDate
            }).ToList()
        };

        // Подтягиваем алиасы прямо из доменной модели через репозиторий,
        // чтобы экран Review мог показывать "Answers + aliases".
        var questionWithAliases = await _unitOfWork.Questions.GetWithAllDetailsAsync(questionId, HttpContext.RequestAborted);
        if (questionWithAliases != null)
        {
            dto.MediaUrl = ToPublicMediaUrl(questionWithAliases.MediaAssets.FirstOrDefault()?.Url);
            dto.Tags = questionWithAliases.Tags
                .Select(qt => qt.Tag.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            dto.Hints = questionWithAliases.Hints
                .OrderBy(h => h.OrderIndex)
                .Select(h => new ModerationHintDto
                {
                    Id = h.Id,
                    OrderIndex = h.OrderIndex,
                    HintText = h.HintText,
                    RevealTimeSeconds = h.RevealTimeSec
                })
                .ToList();

            dto.Answers = questionWithAliases.Answers.Select(a => new ModerationAnswerDto
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                IsPrimary = a.IsPrimary,
                AllowFuzzyMatch = a.AllowFuzzyMatch,
                MinConfidence = a.MinConfidence,
                MaxEditDistance = a.MaxEditDistance,
                Aliases = a.Aliases.Select(al => new ModerationAliasDto
                {
                    Id = al.Id,
                    AliasText = al.AliasText,
                    Kind = al.Kind.ToString()
                }).ToList()
            }).ToList();
        }

        return Ok(dto);
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
            return BadRequest("Invalid user ID");
        }

        var stats = await _mediator.Send(new GetModeratorStatsQuery { ModeratorId = moderatorId });
        return Ok(stats);
    }

    private string? ToPublicMediaUrl(string? mediaUrl)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
            return mediaUrl;

        if (Uri.TryCreate(mediaUrl, UriKind.Absolute, out _))
            return mediaUrl;

        if (!mediaUrl.StartsWith('/'))
            mediaUrl = $"/{mediaUrl}";

        var request = HttpContext.Request;
        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}{mediaUrl}";
    }

    private static string ToClientStatus(QuestionStatus status)
    {
        // UI клиента частично ожидает UPPERCASE для approved/rejected.
        return status switch
        {
            QuestionStatus.Approved => "APPROVED",
            QuestionStatus.Rejected => "REJECTED",
            _ => status.ToString()
        };
    }
}

public record SuggestQuestionRequest
{
    public string PromptText { get; init; } = string.Empty;
    public QuizFuzz.Domain.Enums.QuestionType Type { get; init; }
    public QuizFuzz.Domain.Enums.Difficulty Difficulty { get; init; }
    public List<AnswerWithFuzzySettings> CorrectAnswers { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
    public string? MediaUrl { get; init; }
    public string? Title { get; init; }
    public string? Explanation { get; init; }
    public List<QuizFuzz.Shared.Dtos.Questions.CreateHintRequest> Hints { get; init; } = new();
}

public record AnswerWithFuzzySettings
{
    public string Text { get; init; } = string.Empty;
    public bool AllowFuzzyMatch { get; init; } = true;
    public double MinConfidence { get; init; } = 0.7;
}
