using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с тегами
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
    /// Получить все активные теги
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTags()
    {
        var tags = await _unitOfWork.Tags.GetActiveAsync();
        
        var result = tags.Select(t => new
        {
            t.Id,
            t.Name,
            t.Description,
            t.IsActive,
            t.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Получить тег по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTag(Guid id)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id);
        
        if (tag == null)
            return NotFound($"Tag with ID {id} not found");

        return Ok(new
        {
            tag.Id,
            tag.Name,
            tag.Description,
            tag.IsActive,
            tag.CreatedAt
        });
    }

    /// <summary>
    /// Создать новый тег
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Tag name is required");

        // Check if tag with same name exists
        if (await _unitOfWork.Tags.IsNameTakenAsync(request.Name))
            return BadRequest($"Tag with name '{request.Name}' already exists");

        var tag = new Tag(request.Name, request.Description);

        await _unitOfWork.Tags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Tag {TagName} created with ID {TagId}", tag.Name, tag.Id);

        return CreatedAtAction(
            nameof(GetTag),
            new { id = tag.Id },
            new
            {
                tag.Id,
                tag.Name,
                tag.Description,
                tag.IsActive,
                tag.CreatedAt
            });
    }

    /// <summary>
    /// Обновить тег
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagRequest request)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id);
        
        if (tag == null)
            return NotFound($"Tag with ID {id} not found");

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != tag.Name)
        {
            if (await _unitOfWork.Tags.IsNameTakenAsync(request.Name))
                return BadRequest($"Tag with name '{request.Name}' already exists");
            
            tag.UpdateName(request.Name);
        }

        if (request.Description != null)
        {
            tag.UpdateDescription(request.Description);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Tag {TagId} updated", id);

        return Ok(new
        {
            tag.Id,
            tag.Name,
            tag.Description,
            tag.IsActive,
            tag.CreatedAt
        });
    }

    /// <summary>
    /// Деактивировать тег
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTag(Guid id)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id);
        
        if (tag == null)
            return NotFound($"Tag with ID {id} not found");

        tag.Deactivate();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Tag {TagId} deactivated", id);

        return NoContent();
    }
}

public record CreateTagRequest(string Name, string? Description);
public record UpdateTagRequest(string? Name, string? Description);
