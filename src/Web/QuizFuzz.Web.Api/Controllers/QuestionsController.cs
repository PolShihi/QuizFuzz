using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Application.Questions.Commands.CreateQuestion;
using QuizFuzz.Application.Questions.Queries.GetQuestion;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с вопросами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<QuestionsController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Получить список всех одобренных вопросов
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions([FromQuery] QuestionStatus? status = null)
    {
        IReadOnlyList<Question> questions;

        if (status.HasValue)
        {
            questions = await _unitOfWork.Questions.GetByStatusAsync(status.Value);
        }
        else
        {
            // По умолчанию только одобренные вопросы
            questions = await _unitOfWork.Questions.GetByStatusAsync(QuestionStatus.Approved);
        }

        var result = questions.Select(q => new
        {
            q.Id,
            q.Type,
            q.Title,
            q.PromptText,
            q.Difficulty,
            q.LanguageCode,
            q.Status,
            q.AuthorUserId,
            q.CreatedAt,
            q.UpdatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Получить вопрос по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestion(Guid id)
    {
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(id);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        var result = new QuestionDto
        {
            Id = question.Id,
            Type = question.Type,
            Title = question.Title,
            PromptText = question.PromptText,
            Difficulty = question.Difficulty,
            LanguageCode = question.LanguageCode,
            Status = question.Status,
            AuthorUserId = question.AuthorUserId,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Answers = question.Answers.Select(a => new AnswerDto
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                IsPrimary = a.IsPrimary,
                IsActive = a.IsActive,
                Aliases = a.Aliases.Select(alias => new AliasDto
                {
                    Id = alias.Id,
                    AliasText = alias.AliasText,
                    Kind = alias.Kind
                }).ToList()
            }).ToList(),
            Hints = question.Hints.Select(h => new HintDto
            {
                Id = h.Id,
                OrderIndex = h.OrderIndex,
                HintText = h.HintText ?? "",
                RevealTimeSec = h.RevealTimeSec
            }).ToList(),
            Tags = question.Tags.Select(qt => new TagDto
            {
                Id = qt.Tag.Id,
                Name = qt.Tag.Name,
                Description = qt.Tag.Description
            }).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Создать новый вопрос
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.PromptText))
            return BadRequest("Prompt text is required");

        if (command.Answers == null || !command.Answers.Any())
            return BadRequest("At least one answer is required");

        if (!command.Answers.Any(a => a.IsPrimary))
            return BadRequest("At least one primary answer is required");

        try
        {
            var userId = _currentUserService.UserId;

            var question = new Question(
                command.Type,
                command.PromptText,
                command.Difficulty,
                command.LanguageCode,
                command.Title,
                userId);

            // Добавляем ответы
            foreach (var answerDto in command.Answers)
            {
                var answer = question.AddAnswer(answerDto.AnswerText, answerDto.IsPrimary);

                // Добавляем алиасы
                foreach (var aliasDto in answerDto.Aliases)
                {
                    answer.AddAlias(aliasDto.AliasText, aliasDto.Kind);
                }
            }

            // Добавляем подсказки
            foreach (var hintDto in command.Hints)
            {
                question.AddHint(hintDto.OrderIndex, hintDto.HintText, hintDto.RevealTimeSec);
            }

            // Добавляем теги
            foreach (var tagId in command.TagIds)
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
                if (tag != null)
                {
                    question.AddTag(tag);
                }
            }

            await _unitOfWork.Questions.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Question {QuestionId} created by user {UserId}", question.Id, userId);

            return CreatedAtAction(
                nameof(GetQuestion),
                new { id = question.Id },
                new { question.Id, question.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Обновить вопрос
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionRequest request)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(id);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        var userId = _currentUserService.UserId;

        // Проверка прав: автор или админ
        if (question.AuthorUserId != userId && !_currentUserService.IsInRole("Admin"))
            return Forbid();

        try
        {
            if (!string.IsNullOrWhiteSpace(request.PromptText))
            {
                question.UpdatePrompt(request.PromptText);
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                question.UpdateTitle(request.Title);
            }

            if (request.Difficulty.HasValue)
            {
                question.UpdateDifficulty(request.Difficulty.Value);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Question {QuestionId} updated by user {UserId}", id, userId);

            return Ok(new { message = "Question updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Отправить вопрос на модерацию
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForReview(Guid id)
    {
        var question = await _unitOfWork.Questions.GetWithAnswersAsync(id);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        var userId = _currentUserService.UserId;

        if (question.AuthorUserId != userId && !_currentUserService.IsInRole("Admin"))
            return Forbid();

        try
        {
            question.SubmitForReview();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Question {QuestionId} submitted for review by user {UserId}", id, userId);

            return Ok(new { message = "Question submitted for review", status = question.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Одобрить вопрос (модератор/админ)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveQuestion(Guid id)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(id);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        try
        {
            question.Approve();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Question {QuestionId} approved by {UserId}", id, _currentUserService.UserId);

            return Ok(new { message = "Question approved", status = question.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Отклонить вопрос (модератор/админ)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectQuestion(Guid id)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(id);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        try
        {
            question.Reject();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Question {QuestionId} rejected by {UserId}", id, _currentUserService.UserId);

            return Ok(new { message = "Question rejected", status = question.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить вопросы по тегам
    /// </summary>
    [HttpGet("by-tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestionsByTags([FromQuery] Guid[] tagIds)
    {
        if (tagIds == null || tagIds.Length == 0)
            return BadRequest("At least one tag ID is required");

        var questions = await _unitOfWork.Questions.GetApprovedByTagsAsync(tagIds);

        var result = questions.Select(q => new
        {
            q.Id,
            q.Type,
            q.Title,
            q.PromptText,
            q.Difficulty,
            q.LanguageCode
        });

        return Ok(result);
    }

    /// <summary>
    /// Получить случайный вопрос
    /// </summary>
    [HttpGet("random")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRandomQuestion([FromQuery] Guid[]? tagIds = null)
    {
        var question = await _unitOfWork.Questions.GetRandomApprovedAsync(tagIds);

        if (question == null)
            return NotFound("No approved questions found");

        return Ok(new
        {
            question.Id,
            question.Type,
            question.Title,
            question.PromptText,
            question.Difficulty,
            question.LanguageCode
        });
    }
}

public record UpdateQuestionRequest(
    string? PromptText,
    string? Title,
    Difficulty? Difficulty);
