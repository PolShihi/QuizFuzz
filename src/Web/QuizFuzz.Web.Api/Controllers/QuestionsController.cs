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
    public async Task<IActionResult> GetQuestions(
        [FromQuery] QuestionStatus? status = null,
        [FromQuery] Difficulty? difficulty = null,
        [FromQuery] Guid[]? tagIds = null)
    {
        var effectiveStatus = status ?? QuestionStatus.Approved;

        var questions = await _unitOfWork.Questions.GetForModerationQueueAsync(
            effectiveStatus,
            limit: 1000,
            cancellationToken: HttpContext.RequestAborted);

        if (difficulty.HasValue)
        {
            questions = questions.Where(q => q.Difficulty == difficulty.Value).ToList();
        }

        if (tagIds is { Length: > 0 })
        {
            var selectedTagIds = tagIds.Where(id => id != Guid.Empty).ToHashSet();
            if (selectedTagIds.Count > 0)
            {
                questions = questions
                    .Where(q => q.Tags.Any(qt => selectedTagIds.Contains(qt.TagId)))
                    .ToList();
            }
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
            AuthorUsername = q.Author?.Username,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
            Answers = new List<QuestionAnswerDto>(),
            AnswersCount = q.Answers?.Count ?? 0,
            Hints = new List<HintDto>(),
            Tags = (q.Tags ?? Array.Empty<QuestionTag>())
                .Where(qt => qt.Tag != null)
                .OrderBy(qt => qt.Tag.Name)
                .Select(qt => new TagDto
                {
                    Id = qt.Tag.Id,
                    Name = qt.Tag.Name,
                    Description = qt.Tag.Description,
                    IsActive = qt.Tag.IsActive
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Получить вопрос по ID. Для совместимости возвращает полный DTO.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestion(Guid id)
    {
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(id, HttpContext.RequestAborted);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        return Ok(MapQuestionToDto(question));
    }

    /// <summary>
    /// Получить полный вопрос для админ-панели: ответы, алиасы, теги, подсказки и media.
    /// </summary>
    [HttpGet("{id:guid}/full")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionFull(Guid id)
    {
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(id, HttpContext.RequestAborted);

        if (question == null)
            return NotFound($"Question with ID {id} not found");

        return Ok(MapQuestionToDto(question));
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
                //  НОВОЕ: Передаем настройки fuzzy matching из DTO
                var answer = question.AddAnswer(
                    answerDto.AnswerText, 
                    answerDto.IsPrimary,
                    null, // languageCode
                    answerDto.AllowFuzzyMatch,
                    answerDto.MaxEditDistance,
                    answerDto.MinConfidence);
                    
                _logger.LogDebug("Added answer '{Text}' with fuzzy settings: Allow={Allow}, MaxDist={MaxDist}, MinConf={MinConf}",
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
                    _ => "OTHER"
                };
                
                question.AddMediaAsset(mediaType, request.MediaUrl, "FileSystem");
                _logger.LogDebug(" [CreateQuestion] Added media asset: {Type} - {Url}", mediaType, request.MediaUrl);
            }

            await _unitOfWork.Questions.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Question {QuestionId} created by user {UserId}", question.Id, userId);

            // Return created question with DTO
            var createdQuestion = await _unitOfWork.Questions.GetWithAllDetailsAsync(question.Id);
            
            var resultDto = MapQuestionToDto(createdQuestion!);
            
            return CreatedAtAction(
                nameof(GetQuestion),
                new { id = question.Id },
                resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error creating question");
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

            _logger.LogDebug("Question {QuestionId} updated by user {UserId}", id, userId);

            return Ok(new { message = "Question updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating question {QuestionId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Полное обновление вопроса (для админ-панели: просмотр/редактирование ответов/алиасов/тегов)
    /// </summary>
    [HttpPut("{id:guid}/full")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateQuestionFull(Guid id, [FromBody] UpdateQuestionFullRequest request)
    {
        var question = await _unitOfWork.Questions.GetWithAllDetailsAsync(id, HttpContext.RequestAborted);
        if (question == null)
        {
            return NotFound($"Question with ID {id} not found");
        }

        if (string.IsNullOrWhiteSpace(request.PromptText) || request.PromptText.Length < 10)
        {
            return BadRequest("PromptText must be at least 10 characters");
        }

        if (request.Answers == null || request.Answers.Count == 0)
        {
            return BadRequest("At least one answer is required");
        }

        if (!request.Answers.Any(a => a.IsPrimary))
        {
            return BadRequest("At least one primary answer is required");
        }

        if (!Enum.TryParse<Difficulty>(request.Difficulty, true, out var parsedDifficulty))
        {
            return BadRequest($"Invalid difficulty: {request.Difficulty}");
        }

        // Basic fields
        question.UpdateTitle(request.Title);
        question.UpdatePrompt(request.PromptText);
        question.UpdateDifficulty(parsedDifficulty);

        // Tags: make it match request.TagIds
        var desiredTagIds = new HashSet<Guid>(request.TagIds ?? new List<Guid>());
        var currentTagIds = question.Tags.Select(t => t.TagId).ToList();

        foreach (var tagId in currentTagIds)
        {
            if (!desiredTagIds.Contains(tagId))
            {
                question.RemoveTag(tagId);
            }
        }

        if (desiredTagIds.Count > 0)
        {
            var tags = await _unitOfWork.Tags.GetAllAsync(HttpContext.RequestAborted);
            foreach (var tagId in desiredTagIds)
            {
                var tag = tags.FirstOrDefault(t => t.Id == tagId);
                if (tag != null)
                {
                    question.AddTag(tag);
                }
            }
        }

        // Answers: update existing + add new first, then delete removed.
        var originalExistingAnswerIds = question.Answers.Select(a => a.Id).ToHashSet();
        var existingAnswersById = question.Answers.ToDictionary(a => a.Id, a => a);
        var requestedExistingAnswerIds = request.Answers.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();

        // Update/add answers
        foreach (var answerReq in request.Answers)
        {
            if (answerReq.Id.HasValue && existingAnswersById.TryGetValue(answerReq.Id.Value, out var existing))
            {
                existing.UpdateAnswerText(answerReq.AnswerText);
                if (answerReq.IsPrimary) existing.SetAsPrimary(); else existing.SetAsAlternative();
                existing.UpdateFuzzyMatchSettings(answerReq.AllowFuzzyMatch, answerReq.MaxEditDistance, answerReq.MinConfidence);

                // Aliases: sync by text (simple approach)
                var desiredAliases = answerReq.Aliases ?? new List<UpdateAliasRequest>();
                var desiredNormalized = desiredAliases
                    .Where(a => !string.IsNullOrWhiteSpace(a.AliasText))
                    .Select(a => new { Text = a.AliasText.Trim(), Kind = a.Kind })
                    .ToList();

                var existingAliasIds = existing.Aliases.Select(a => a.Id).ToHashSet();

                // Remove aliases not present by id when provided, else by text match
                var desiredById = desiredAliases.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();
                foreach (var alias in existing.Aliases.ToList())
                {
                    if (desiredById.Count > 0)
                    {
                        if (!desiredById.Contains(alias.Id))
                        {
                            existing.RemoveAlias(alias.Id);
                        }
                    }
                }

                // Add missing aliases
                foreach (var aliasReq in desiredAliases)
                {
                    if (string.IsNullOrWhiteSpace(aliasReq.AliasText))
                        continue;

                    if (aliasReq.Id.HasValue && existingAliasIds.Contains(aliasReq.Id.Value))
                        continue;

                    if (!Enum.TryParse<AliasKind>(aliasReq.Kind, true, out var aliasKind))
                        aliasKind = AliasKind.Synonym;

                    // AddAlias checks duplicates by normalized value
                    existing.AddAlias(aliasReq.AliasText, aliasKind);
                }
            }
            else
            {
                var created = question.AddAnswer(
                    answerReq.AnswerText,
                    isPrimary: answerReq.IsPrimary,
                    languageCode: question.LanguageCode,
                    allowFuzzyMatch: answerReq.AllowFuzzyMatch,
                    maxEditDistance: answerReq.MaxEditDistance,
                    minConfidence: answerReq.MinConfidence);

                foreach (var aliasReq in answerReq.Aliases ?? new List<UpdateAliasRequest>())
                {
                    if (string.IsNullOrWhiteSpace(aliasReq.AliasText))
                        continue;

                    if (!Enum.TryParse<AliasKind>(aliasReq.Kind, true, out var aliasKind))
                        aliasKind = AliasKind.Synonym;

                    created.AddAlias(aliasReq.AliasText, aliasKind);
                }
            }
        }

        // Hints: update existing + add new first, then delete removed.
        var requestedHints = (request.Hints ?? new List<UpdateHintRequest>())
            .Where(h => !string.IsNullOrWhiteSpace(h.HintText))
            .OrderBy(h => h.RevealTimeSeconds)
            .ThenBy(h => h.OrderIndex)
            .Select((h, index) => new
            {
                Hint = h,
                NormalizedOrderIndex = index
            })
            .ToList();

        var existingHintsById = question.Hints.ToDictionary(h => h.Id, h => h);
        var requestedExistingHintIds = requestedHints
            .Where(h => h.Hint.Id.HasValue)
            .Select(h => h.Hint.Id!.Value)
            .ToHashSet();

        foreach (var requestedHint in requestedHints)
        {
            var hintReq = requestedHint.Hint;
            if (hintReq.Id.HasValue && existingHintsById.TryGetValue(hintReq.Id.Value, out var existingHint))
            {
                existingHint.UpdateOrderIndex(requestedHint.NormalizedOrderIndex);
                existingHint.UpdateHintText(hintReq.HintText);
                existingHint.UpdateRevealTime(hintReq.RevealTimeSeconds);
            }
            else
            {
                question.AddHint(
                    requestedHint.NormalizedOrderIndex,
                    hintReq.HintText.Trim(),
                    hintReq.RevealTimeSeconds);
            }
        }

        foreach (var existingHint in question.Hints.ToList())
        {
            if (!requestedExistingHintIds.Contains(existingHint.Id) &&
                !requestedHints.Any(h => !h.Hint.Id.HasValue &&
                    string.Equals(h.Hint.HintText.Trim(), existingHint.HintText, StringComparison.OrdinalIgnoreCase) &&
                    h.Hint.RevealTimeSeconds == existingHint.RevealTimeSec))
            {
                question.RemoveHint(existingHint.Id);
            }
        }

        // Ensure primary exists before deletions
        if (!question.Answers.Any(a => a.IsPrimary))
        {
            return BadRequest("Invalid update: question must have at least one primary answer");
        }

        // Delete answers that were present before but removed from request
        foreach (var existingId in originalExistingAnswerIds)
        {
            if (!requestedExistingAnswerIds.Contains(existingId))
            {
                question.RemoveAnswer(existingId);
            }
        }

        await _unitOfWork.SaveChangesAsync(HttpContext.RequestAborted);

        // Return refreshed question dto
        var refreshed = await _unitOfWork.Questions.GetWithAllDetailsAsync(id, HttpContext.RequestAborted);
        if (refreshed == null)
        {
            return NotFound();
        }

        return Ok(MapQuestionToDto(refreshed));
    }

    private QuestionDto MapQuestionToDto(Question question)
    {
        var answers = question.Answers?
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.AnswerText)
            .Select(a => new QuestionAnswerDto
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                IsPrimary = a.IsPrimary,
                IsActive = a.IsActive,
                AllowFuzzyMatch = a.AllowFuzzyMatch,
                MaxEditDistance = a.MaxEditDistance,
                MinConfidence = a.MinConfidence,
                Aliases = (a.Aliases ?? Array.Empty<FuzzyAlias>()).Select(alias => new FuzzyAliasDto
                {
                    Id = alias.Id,
                    AliasText = alias.AliasText,
                    Kind = alias.Kind.ToString()
                }).ToList()
            }).ToList() ?? new List<QuestionAnswerDto>();

        return new QuestionDto
        {
            Id = question.Id,
            QuestionType = question.Type.ToString(),
            Title = question.Title ?? string.Empty,
            PromptText = question.PromptText,
            Difficulty = question.Difficulty.ToString(),
            LanguageCode = question.LanguageCode,
            Status = question.Status.ToString(),
            AuthorUserId = question.AuthorUserId,
            AuthorUsername = question.Author?.Username,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Answers = answers,
            AnswersCount = answers.Count,
            Hints = (question.Hints ?? Array.Empty<Hint>()).OrderBy(h => h.OrderIndex).Select(h => new HintDto
            {
                Id = h.Id,
                OrderIndex = h.OrderIndex,
                HintText = h.HintText ?? string.Empty,
                RevealTimeSeconds = h.RevealTimeSec
            }).ToList(),
            Tags = (question.Tags ?? Array.Empty<QuestionTag>())
                .Where(qt => qt.Tag != null)
                .OrderBy(qt => qt.Tag.Name)
                .Select(qt => new TagDto
                {
                    Id = qt.Tag.Id,
                    Name = qt.Tag.Name,
                    Description = qt.Tag.Description,
                    IsActive = qt.Tag.IsActive
                }).ToList(),
            MediaUrl = ToPublicMediaUrl(question.MediaAssets?.FirstOrDefault()?.Url)
        };
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

    /// <summary>
    /// Удалить вопрос (для админ-панели)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(id, HttpContext.RequestAborted);
        if (question == null)
        {
            return NotFound($"Question with ID {id} not found");
        }

        _unitOfWork.Questions.Remove(question);
        await _unitOfWork.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(new { message = "Question deleted successfully" });
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

            _logger.LogDebug("Question {QuestionId} submitted for review by user {UserId}", id, userId);

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

            _logger.LogDebug("Question {QuestionId} approved by {UserId}", id, _currentUserService.UserId);

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

            _logger.LogDebug("Question {QuestionId} rejected by {UserId}", id, _currentUserService.UserId);

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
