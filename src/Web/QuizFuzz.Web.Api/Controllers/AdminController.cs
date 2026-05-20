using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Admin;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для администрирования системы
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUnitOfWork unitOfWork,
        ILogger<AdminController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Получить список пользователей (для админки клиента)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserManagementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);

        var items = users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserManagementDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email.Value,
                Roles = u.Roles.Select(r => r.ToString()).ToList(),
                IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToList();

        return Ok(items);
    }

    /// <summary>
    /// Получить пользователя (для админки клиента)
    /// </summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Ok(new UserManagementDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email.Value,
            Roles = user.Roles.Select(r => r.ToString()).ToList(),
            IsBanned = user.IsBanned,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    /// <summary>
    /// Обновить роли пользователя (для админки клиента)
    /// </summary>
    [HttpPut("users/{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(
        Guid id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        // Нормализуем желаемые роли, всегда оставляем базовую роль User.
        var desiredRoleStrings = (request.Roles ?? new List<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToList();

        var desiredRoles = new HashSet<UserRole>();
        foreach (var roleStr in desiredRoleStrings)
        {
            if (!Enum.TryParse<UserRole>(roleStr, true, out var parsedRole))
            {
                return BadRequest($"Invalid role: {roleStr}");
            }

            desiredRoles.Add(parsedRole);
        }

        desiredRoles.Add(UserRole.User);

        // Удаляем роли, которых не должно быть.
        foreach (var existing in user.Roles.ToList())
        {
            if (!desiredRoles.Contains(existing))
            {
                user.RemoveRole(existing);
            }
        }

        // Добавляем недостающие роли.
        foreach (var role in desiredRoles)
        {
            user.AddRole(role);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Roles updated successfully",
            roles = user.Roles.Select(r => r.ToString()).ToList()
        });
    }

    /// <summary>
    /// Заблокировать пользователя
    /// </summary>
    [HttpPost("users/{id}/ban")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid id, [FromBody] BanUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found");

        try
        {
            user.Ban(request.BannedUntil);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning("User {UserId} ({Username}) banned until {BanUntil}", 
                id, user.Username, request.BannedUntil);

            return Ok(new 
            { 
                message = "User banned successfully",
                bannedUntil = request.BannedUntil,
                reason = request.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error banning user {UserId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Разблокировать пользователя
    /// </summary>
    [HttpPost("users/{id}/unban")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbanUser(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found");

        try
        {
            user.Unban();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} ({Username}) unbanned", id, user.Username);

            return Ok(new { message = "User unbanned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbanning user {UserId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Добавить роль пользователю
    /// </summary>
    [HttpPost("users/{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRole(Guid id, [FromBody] ManageRoleRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found");

        try
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
                return BadRequest($"Invalid role: {request.Role}");

            user.AddRole(role);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Role {Role} added to user {UserId} ({Username})", 
                role, id, user.Username);

            return Ok(new 
            { 
                message = $"Role {role} added successfully",
                roles = user.Roles.Select(r => r.ToString())
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role to user {UserId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Удалить роль у пользователя
    /// </summary>
    [HttpDelete("users/{id}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, string role)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found");

        try
        {
            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
                return BadRequest($"Invalid role: {role}");

            user.RemoveRole(userRole);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Role {Role} removed from user {UserId} ({Username})", 
                userRole, id, user.Username);

            return Ok(new 
            { 
                message = $"Role {userRole} removed successfully",
                roles = user.Roles.Select(r => r.ToString())
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить системную статистику
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemStats()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var questions = await _unitOfWork.Questions.GetAllAsync();
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var sessions = await _unitOfWork.GameSessions.GetAllAsync();
        var tags = await _unitOfWork.Tags.GetAllAsync();

        var stats = new
        {
            users = new
            {
                total = users.Count,
                active = users.Count(u => !u.IsBanned),
                banned = users.Count(u => u.IsBanned),
                admins = users.Count(u => u.Roles.Contains(UserRole.Admin)),
                moderators = users.Count(u => u.Roles.Contains(UserRole.Moderator))
            },
            questions = new
            {
                total = questions.Count,
                approved = questions.Count(q => q.Status == QuestionStatus.Approved),
                pending = questions.Count(q => q.Status == QuestionStatus.UnderReview),
                rejected = questions.Count(q => q.Status == QuestionStatus.Rejected),
                draft = questions.Count(q => q.Status == QuestionStatus.Draft)
            },
            rooms = new
            {
                total = rooms.Count,
                active = rooms.Count(r => r.Status == RoomStatus.InProgress),
                lobby = rooms.Count(r => r.Status == RoomStatus.Lobby),
                finished = rooms.Count(r => r.Status == RoomStatus.Finished)
            },
            sessions = new
            {
                total = sessions.Count,
                active = sessions.Count(s => s.Status == GameSessionStatus.Active),
                finished = sessions.Count(s => s.Status == GameSessionStatus.Finished)
            },
            tags = new
            {
                total = tags.Count,
                active = tags.Count(t => t.IsActive)
            }
        };

        return Ok(stats);
    }

    /// <summary>
    /// Получить недавние действия (упрощенный аудит-лог)
    /// </summary>
    [HttpGet("recent-actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActions([FromQuery] int limit = 50)
    {
        // Получаем недавние вопросы
        var recentQuestions = await _unitOfWork.Questions.GetAllAsync();
        var questionActions = recentQuestions
            .OrderByDescending(q => q.UpdatedAt)
            .Take(limit / 2)
            .Select(q => new RecentAction
            {
                Type = "question",
                Action = q.Status.ToString(),
                EntityId = q.Id,
                EntityName = q.Title ?? q.PromptText.Substring(0, Math.Min(50, q.PromptText.Length)),
                UserId = q.AuthorUserId,
                Timestamp = q.UpdatedAt
            });

        // Получаем недавние комнаты
        var recentRooms = await _unitOfWork.Rooms.GetAllAsync();
        var roomActions = recentRooms
            .OrderByDescending(r => r.UpdatedAt)
            .Take(limit / 2)
            .Select(r => new RecentAction
            {
                Type = "room",
                Action = r.Status.ToString(),
                EntityId = r.Id,
                EntityName = r.Name,
                UserId = r.OwnerUserId,
                Timestamp = r.UpdatedAt
            });

        var allActions = questionActions
            .Concat(roomActions)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit);

        return Ok(new
        {
            actions = allActions
        });
    }
}

public record ManageRoleRequest(string Role);

public class RecentAction
{
    public string Type { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; }
}
