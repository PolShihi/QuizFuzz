using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с приглашениями в комнаты
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<InvitationsController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Создать приглашение в комнату
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var room = await _unitOfWork.Rooms.GetByIdAsync(request.RoomId);
        if (room == null)
            return NotFound($"Room with ID {request.RoomId} not found");

        // Только владелец может создавать приглашения
        if (room.OwnerUserId != userId.Value)
            return Forbid();

        try
        {
            // Генерируем уникальный код приглашения
            var code = GenerateInvitationCode();
            var expiresAt = request.ExpiresInHours.HasValue 
                ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value)
                : DateTime.UtcNow.AddDays(7); // По умолчанию 7 дней

            var invitation = new Invitation(
                request.RoomId,
                code,
                expiresAt,
                userId.Value);

            await _unitOfWork.Invitations.AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Invitation {Code} created for room {RoomId} by user {UserId}", 
                code, request.RoomId, userId);

            return CreatedAtAction(
                nameof(GetInvitation),
                new { code },
                new
                {
                    invitation.Id,
                    invitation.Code,
                    invitation.RoomId,
                    roomName = room.Name,
                    invitation.ExpiresAt,
                    invitation.CreatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error creating invitation for room {RoomId}", request.RoomId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить информацию о приглашении по коду
    /// </summary>
    [HttpGet("{code}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitation(string code)
    {
        var invitation = await _unitOfWork.Invitations.GetByCodeAsync(code);
        
        if (invitation == null)
            return NotFound("Invitation not found");

        if (invitation.IsExpired)
            return BadRequest("Invitation has expired");

        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(invitation.RoomId);
        if (room == null)
            return NotFound("Room not found");

        return Ok(new
        {
            invitation.Code,
            invitation.RoomId,
            roomName = room.Name,
            roomVisibility = room.Visibility.ToString(),
            maxPlayers = room.MaxPlayers,
            currentPlayers = 0, // TODO: подсчитать из активной сессии
            ownerUsername = room.Owner.Username,
            invitation.ExpiresAt,
            isExpired = invitation.IsExpired
        });
    }

    /// <summary>
    /// Принять приглашение и присоединиться к комнате
    /// </summary>
    [HttpPost("{code}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation(string code)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var invitation = await _unitOfWork.Invitations.GetByCodeAsync(code);
        
        if (invitation == null)
            return NotFound("Invitation not found");

        if (invitation.IsExpired)
            return BadRequest("Invitation has expired");

        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(invitation.RoomId);
        if (room == null)
            return NotFound("Room not found");

        if (room.Status != RoomStatus.Lobby)
            return BadRequest("Can only join rooms in lobby status");

        try
        {
            // Получаем или создаем активную сессию
            var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(invitation.RoomId);

            if (session == null)
            {
                session = new GameSession(invitation.RoomId, 10);
                await _unitOfWork.GameSessions.AddAsync(session);
            }

            // Проверяем, не присоединился ли уже игрок
            var existingPlayer = session.Players.FirstOrDefault(p => p.UserId == userId.Value && p.IsActive);
            if (existingPlayer != null)
                return BadRequest("You are already in this room");

            // Проверяем лимит игроков
            var activePlayers = session.Players.Count(p => p.IsActive);
            if (activePlayers >= room.MaxPlayers)
                return BadRequest("Room is full");

            // Добавляем игрока
            var isOwner = room.OwnerUserId == userId.Value;
            session.AddPlayer(userId.Value, isOwner);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("User {UserId} joined room {RoomId} via invitation {Code}", 
                userId, invitation.RoomId, code);

            return Ok(new
            {
                message = "Successfully joined room",
                sessionId = session.Id,
                roomId = invitation.RoomId,
                roomName = room.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error accepting invitation {Code}", code);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить все приглашения комнаты (только владелец)
    /// </summary>
    [HttpGet("room/{roomId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoomInvitations(Guid roomId)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
            return NotFound($"Room with ID {roomId} not found");

        if (room.OwnerUserId != userId.Value)
            return Forbid();

        var invitations = await _unitOfWork.Invitations.GetByRoomIdAsync(roomId);

        var result = invitations
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.Code,
                i.CreatedAt,
                i.ExpiresAt,
                isExpired = i.IsExpired,
                createdBy = i.CreatedByUserId
            });

        return Ok(new
        {
            roomId,
            invitations = result
        });
    }

    /// <summary>
    /// Удалить/отозвать приглашение (только владелец)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvitation(Guid id)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(id);
        if (invitation == null)
            return NotFound($"Invitation with ID {id} not found");

        var room = await _unitOfWork.Rooms.GetByIdAsync(invitation.RoomId);
        if (room == null)
            return NotFound("Room not found");

        if (room.OwnerUserId != userId.Value)
            return Forbid();

        _unitOfWork.Invitations.Remove(invitation);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogDebug("Invitation {InvitationId} deleted by user {UserId}", id, userId);

        return NoContent();
    }

    private static string GenerateInvitationCode()
    {
        // Генерируем читаемый код из 8 символов (без похожих 0/O, 1/I/l)
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

public record CreateInvitationRequest(Guid RoomId, int? ExpiresInHours);
