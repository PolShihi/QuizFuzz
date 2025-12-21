using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Application.Rooms.Commands.CreateRoom;
using QuizFuzz.Application.Rooms.Queries.GetRoom;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для работы с игровыми комнатами
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        ILogger<RoomsController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Получить список публичных комнат
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicRooms()
    {
        var rooms = await _unitOfWork.Rooms.GetPublicRoomsAsync();

        var result = rooms.Select(r => new
        {
            r.Id,
            r.Name,
            r.OwnerUserId,
            OwnerUsername = r.Owner.Username,
            r.MaxPlayers,
            CurrentPlayers = 0, // TODO: подсчитать из активной сессии
            r.Status,
            r.VictoryConditionType,
            r.VictoryValue,
            r.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Получить комнату по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
            return NotFound($"Room with ID {id} not found");

        var result = new RoomDto
        {
            Id = room.Id,
            OwnerUserId = room.OwnerUserId,
            OwnerUsername = room.Owner.Username,
            Name = room.Name,
            Visibility = room.Visibility,
            MaxPlayers = room.MaxPlayers,
            CurrentPlayers = 0, // TODO: подсчитать
            VictoryConditionType = room.VictoryConditionType,
            VictoryValue = room.VictoryValue,
            TagSelectionMode = room.TagSelectionMode,
            Status = room.Status,
            CreatedAt = room.CreatedAt,
            Tags = room.TagSelections.Select(ts => ts.Tag.Name).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Создать новую комнату
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return BadRequest("Room name is required");

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            string? accessCodeHash = null;
            if (command.Visibility == RoomVisibility.Private && !string.IsNullOrWhiteSpace(command.AccessCode))
            {
                accessCodeHash = _passwordHasher.HashPassword(command.AccessCode);
            }

            var room = new Room(
                userId.Value,
                command.Name,
                command.Visibility,
                command.MaxPlayers,
                command.VictoryConditionType,
                command.VictoryValue,
                command.TagSelectionMode);

            if (accessCodeHash != null)
            {
                room.SetAccessCode(accessCodeHash);
            }

            // Добавляем выбранные теги
            foreach (var tagSelection in command.TagSelections)
            {
                room.AddTagSelection(tagSelection.TagId, tagSelection.Weight);
            }

            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Room {RoomId} created by user {UserId}", room.Id, userId);

            return CreatedAtAction(
                nameof(GetRoom),
                new { id = room.Id },
                new { room.Id, room.Name, room.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Присоединиться к комнате
    /// </summary>
    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinRoom(Guid id, [FromBody] JoinRoomRequest? request = null)
    {
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
            return NotFound($"Room with ID {id} not found");

        if (room.Status != RoomStatus.Lobby)
            return BadRequest("Can only join rooms in lobby status");

        // Проверка access code для приватных комнат
        if (room.Visibility == RoomVisibility.Private && !string.IsNullOrWhiteSpace(room.AccessCodeHash))
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AccessCode))
                return BadRequest("Access code is required for private rooms");

            if (!_passwordHasher.VerifyPassword(request.AccessCode, room.AccessCodeHash))
                return BadRequest("Invalid access code");
        }

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        // Получаем или создаем активную сессию
        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
        {
            // Создаем новую сессию
            session = new GameSession(id, 10); // TODO: настраиваемое количество раундов
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

        _logger.LogInformation("User {UserId} joined room {RoomId}", userId, id);

        return Ok(new
        {
            message = "Successfully joined room",
            sessionId = session.Id,
            roomId = id
        });
    }

    /// <summary>
    /// Покинуть комнату
    /// </summary>
    [HttpPost("{id}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveRoom(Guid id)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
            return NotFound("No active session found for this room");

        session.RemovePlayer(userId.Value);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} left room {RoomId}", userId, id);

        return Ok(new { message = "Successfully left room" });
    }

    /// <summary>
    /// Начать игру в комнате (только владелец)
    /// </summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartGame(Guid id)
    {
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
            return NotFound($"Room with ID {id} not found");

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        // Проверка прав владельца
        if (room.OwnerUserId != userId.Value)
            return Forbid();

        if (room.Status != RoomStatus.Lobby)
            return BadRequest("Game can only be started from lobby");

        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
            return BadRequest("No active session found");

        var activePlayers = session.Players.Count(p => p.IsActive);
        if (activePlayers < 2)
            return BadRequest("At least 2 players are required to start the game");

        try
        {
            room.StartGame();
            session.Start();

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Game started in room {RoomId} by user {UserId}", id, userId);

            return Ok(new
            {
                message = "Game started",
                sessionId = session.Id,
                roomStatus = room.Status,
                sessionStatus = session.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Обновить настройки комнаты (только владелец)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomRequest request)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);

        if (room == null)
            return NotFound($"Room with ID {id} not found");

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        if (room.OwnerUserId != userId.Value)
            return Forbid();

        if (room.Status != RoomStatus.Lobby)
            return BadRequest("Can only update room settings in lobby");

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                room.UpdateName(request.Name);
            }

            if (request.MaxPlayers.HasValue)
            {
                room.UpdateMaxPlayers(request.MaxPlayers.Value);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Room {RoomId} updated by user {UserId}", id, userId);

            return Ok(new { message = "Room updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить список игроков в комнате
    /// </summary>
    [HttpGet("{id}/players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayers(Guid id)
    {
        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
            return NotFound("No active session found for this room");

        var players = session.Players
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.UserId,
                Username = p.User.Username,
                p.IsOwnerSnapshot,
                p.JoinedAt
            });

        return Ok(players);
    }
}

public record JoinRoomRequest(string? AccessCode);
public record UpdateRoomRequest(string? Name, int? MaxPlayers);
