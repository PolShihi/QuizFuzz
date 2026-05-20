using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с тегами.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TagsController> _logger;

    public TagsController(
        IUnitOfWork unitOfWork,
        ILogger<TagsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Получить активные теги для публичных пользовательских сценариев.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTags(CancellationToken cancellationToken)
    {
        var tags = await _unitOfWork.Tags.GetActiveAsync(cancellationToken);
        return Ok(tags.Select(ToDto));
    }

    /// <summary>
    /// Получить все теги, включая предложения/неактивные теги, для панели управления.
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTags(CancellationToken cancellationToken)
    {
        var tags = await _unitOfWork.Tags.GetAllAsync(cancellationToken);

        var result = tags
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Name)
            .Select(ToDto)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Получить тег по ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTag(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            return NotFound($"Tag with ID {id} not found");
        }

        return Ok(ToDto(tag));
    }

    /// <summary>
    /// Создать активный тег. Доступно модераторам и администраторам.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Tag name is required");
        }

        if (await _unitOfWork.Tags.IsNameTakenAsync(name, cancellationToken))
        {
            return BadRequest($"Tag with name '{name}' already exists");
        }

        var tag = new Tag(name, request.Description);

        await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tag {TagName} created with ID {TagId} by {User}", tag.Name, tag.Id, User.Identity?.Name);

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, ToDto(tag));
    }

    /// <summary>
    /// Предложить новый тег. Предложение создаётся как неактивный тег и становится видимым в панели тегов.
    /// </summary>
    [HttpPost("suggestions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuggestTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Tag name is required");
        }

        if (await _unitOfWork.Tags.IsNameTakenAsync(name, cancellationToken))
        {
            return BadRequest($"Tag with name '{name}' already exists or is already suggested");
        }

        var tag = new Tag(name, request.Description);
        tag.Deactivate();

        await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tag suggestion {TagName} created with ID {TagId} by {User}", tag.Name, tag.Id, User.Identity?.Name);

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, ToDto(tag));
    }

    /// <summary>
    /// Обновить только описание тега. Название после создания намеренно не редактируется.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTagDescription(Guid id, [FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            return NotFound($"Tag with ID {id} not found");
        }

        tag.UpdateDescription(request.Description);
        _unitOfWork.Tags.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tag {TagId} description updated by {User}", id, User.Identity?.Name);

        return Ok(ToDto(tag));
    }

    /// <summary>
    /// Активировать предложенный или ранее деактивированный тег.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTag(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            return NotFound($"Tag with ID {id} not found");
        }

        tag.Activate();
        _unitOfWork.Tags.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tag {TagId} activated by {User}", id, User.Identity?.Name);

        return Ok(ToDto(tag));
    }

    /// <summary>
    /// Удалить тег из активного списка. Используется soft-delete/deactivate, чтобы не ломать связанные вопросы и комнаты.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTag(Guid id, CancellationToken cancellationToken)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            return NotFound($"Tag with ID {id} not found");
        }

        tag.Deactivate();
        _unitOfWork.Tags.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tag {TagId} deactivated by {User}", id, User.Identity?.Name);

        return NoContent();
    }

    private static object ToDto(Tag tag) => new
    {
        tag.Id,
        tag.Name,
        tag.Description,
        tag.IsActive,
        tag.CreatedAt
    };

    private static string NormalizeName(string? name) => (name ?? string.Empty).Trim();
}

public record CreateTagRequest(string Name, string? Description);
public record UpdateTagRequest(string? Description);
