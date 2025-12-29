using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Questions;

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

        var result = questions.Select(q => new QuestionDto
        {
            Id = q.Id,
            QuestionType = q.Type.ToString(),
            Title = q.Title ?? string.Empty,
            PromptText = q.PromptText,
            Difficulty = q.Difficulty.ToString(),
            LanguageCode = q.LanguageCode,
            Status = q.Status.ToString(),
            AuthorUserId = q.AuthorUserId,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
            Answers = new List<QuestionAnswerDto>(), // Simplified list endpoint
            Hints = new List<HintDto>(),
            Tags = new List<TagDto>()
        }).ToList();

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
            QuestionType = question.Type.ToString(),
            Title = question.Title ?? string.Empty,
            PromptText = question.PromptText,
            Difficulty = question.Difficulty.ToString(),
            LanguageCode = question.LanguageCode,
            Status = question.Status.ToString(),
            AuthorUserId = question.AuthorUserId,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Answers = question.Answers.Select(a => new QuestionAnswerDto
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                IsPrimary = a.IsPrimary,
                IsActive = a.IsActive,
                AllowFuzzyMatch = a.AllowFuzzyMatch, // 🔥 НОВОЕ
                MaxEditDistance = a.MaxEditDistance, // 🔥 НОВОЕ
                MinConfidence = a.MinConfidence,     // 🔥 НОВОЕ
                Aliases = a.Aliases.Select(alias => new FuzzyAliasDto
                {
                    Id = alias.Id,
                    AliasText = alias.AliasText,
                    Kind = alias.Kind.ToString()
                }).ToList()
            }).ToList(),
            Hints = question.Hints.Select(h => new HintDto
            {
                Id = h.Id,
                OrderIndex = h.OrderIndex,
                HintText = h.HintText ?? "",
                RevealTimeSeconds = h.RevealTimeSec
            }).ToList(),
            Tags = question.Tags.Select(qt => new TagDto
            {
                Id = qt.Tag.Id,
                Name = qt.Tag.Name,
                Description = qt.Tag.Description,
                IsActive = qt.Tag.IsActive
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
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PromptText))
            return BadRequest("Prompt text is required");

        if (request.Answers == null || !request.Answers.Any())
            return BadRequest("At least one answer is required");

        if (!request.Answers.Any(a => a.IsPrimary))
            return BadRequest("At least one primary answer is required");

        try
        {
            var userId = _currentUserService.UserId;

            // Parse enum strings
            if (!Enum.TryParse<QuestionType>(request.QuestionType, true, out var questionType))
                questionType = QuestionType.Text;

            if (!Enum.TryParse<Difficulty>(request.Difficulty, true, out var difficulty))
                difficulty = Difficulty.Medium;

            var question = new Question(
                questionType,
                request.PromptText,
                difficulty,
                request.LanguageCode,
                request.Title,
                userId);

            // Добавляем ответы
            foreach (var answerDto in request.Answers)
            {
                // 🔥 НОВОЕ: Передаем настройки fuzzy matching из DTO
                var answer = question.AddAnswer(
                    answerDto.AnswerText, 
                    answerDto.IsPrimary,
                    null, // languageCode
                    answerDto.AllowFuzzyMatch,
                    answerDto.MaxEditDistance,
                    answerDto.MinConfidence);
                    
                _logger.LogInformation("Added answer '{Text}' with fuzzy settings: Allow={Allow}, MaxDist={MaxDist}, MinConf={MinConf}",
                    answerDto.AnswerText, answerDto.AllowFuzzyMatch, answerDto.MaxEditDistance, answerDto.MinConfidence);

                // Добавляем алиасы
                foreach (var aliasDto in answerDto.Aliases)
                {
                    if (!Enum.TryParse<AliasKind>(aliasDto.Kind, true, out var aliasKind))
                        aliasKind = AliasKind.Synonym;
                    
                    answer.AddAlias(aliasDto.AliasText, aliasKind);
                }
            }

            // Добавляем подсказки
            foreach (var hintDto in request.Hints)
            {
                question.AddHint(hintDto.OrderIndex, hintDto.HintText, hintDto.RevealTimeSeconds);
            }

            // Добавляем теги
            foreach (var tagId in request.TagIds)
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
                if (tag != null)
                {
                    question.AddTag(tag);
                }
            }
            
            // Добавляем медиа-ресурс если есть URL
            if (!string.IsNullOrWhiteSpace(request.MediaUrl))
            {
                string mediaType = questionType switch
                {
                    QuestionType.Image => "IMAGE",
                    QuestionType.Audio => "AUDIO",
                    QuestionType.Video => "VIDEO",
                    _ => "OTHER"
                };
                
                question.AddMediaAsset(mediaType, request.MediaUrl, "FileSystem");
                _logger.LogInformation("📎 [CreateQuestion] Added media asset: {Type} - {Url}", mediaType, request.MediaUrl);
            }

            await _unitOfWork.Questions.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Question {QuestionId} created by user {UserId}", question.Id, userId);

            // Return created question with DTO
            var createdQuestion = await _unitOfWork.Questions.GetWithAllDetailsAsync(question.Id);
            
            var resultDto = new QuestionDto
            {
                Id = createdQuestion!.Id,
                QuestionType = createdQuestion.Type.ToString(),
                Title = createdQuestion.Title ?? string.Empty,
                PromptText = createdQuestion.PromptText,
                Difficulty = createdQuestion.Difficulty.ToString(),
                Status = createdQuestion.Status.ToString(),
                LanguageCode = createdQuestion.LanguageCode,
                AuthorUserId = createdQuestion.AuthorUserId,
                CreatedAt = createdQuestion.CreatedAt,
                UpdatedAt = createdQuestion.UpdatedAt,
                Answers = createdQuestion.Answers.Select(a => new QuestionAnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    IsPrimary = a.IsPrimary,
                    IsActive = a.IsActive,
                    AllowFuzzyMatch = a.AllowFuzzyMatch, // 🔥 НОВОЕ
                    MaxEditDistance = a.MaxEditDistance, // 🔥 НОВОЕ
                    MinConfidence = a.MinConfidence,     // 🔥 НОВОЕ
                    Aliases = a.Aliases.Select(alias => new FuzzyAliasDto
                    {
                        Id = alias.Id,
                        AliasText = alias.AliasText,
                        Kind = alias.Kind.ToString()
                    }).ToList()
                }).ToList(),
                Hints = createdQuestion.Hints.Select(h => new HintDto
                {
                    Id = h.Id,
                    OrderIndex = h.OrderIndex,
                    HintText = h.HintText ?? "",
                    RevealTimeSeconds = h.RevealTimeSec
                }).ToList(),
                Tags = createdQuestion.Tags.Select(qt => new TagDto
                {
                    Id = qt.Tag.Id,
                    Name = qt.Tag.Name,
                    Description = qt.Tag.Description,
                    IsActive = qt.Tag.IsActive
                }).ToList()
            };
            
            return CreatedAtAction(
                nameof(GetQuestion),
                new { id = question.Id },
                resultDto);
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
