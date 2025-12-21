using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Rooms;
using QuizFuzz.Web.Api.Hubs;

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
    private readonly IHubContext<LobbyHub> _lobbyHubContext;

    public RoomsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        ILogger<RoomsController> logger,
        IHubContext<LobbyHub> lobbyHubContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _lobbyHubContext = lobbyHubContext;
    }

    /// <summary>
    /// Получить список всех комнат (публичных и приватных для авторизованных)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms()
    {
        _logger.LogInformation("📋 [GetRooms] Loading public rooms list");
        
        var rooms = await _unitOfWork.Rooms.GetPublicRoomsAsync();
        
        _logger.LogInformation("📋 [GetRooms] Found {Count} rooms", rooms.Count());

        var result = new List<RoomListItemDto>();
        
        foreach (var r in rooms)
        {
            // Получаем активную сессию для подсчета игроков
            var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(r.Id);
            var playerCount = session?.Players.Count(p => p.IsActive) ?? 0;
            
            _logger.LogInformation("📊 [GetRooms] Room '{RoomName}': {Players}/{MaxPlayers} players", 
                r.Name, playerCount, r.MaxPlayers);
            
            result.Add(new RoomListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                OwnerUsername = r.Owner.Username,
                IsPrivate = r.Visibility == RoomVisibility.Private,
                MaxPlayers = r.MaxPlayers,
                CurrentPlayers = playerCount, // ✅ Реальное количество!
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            });
        }
        
        _logger.LogInformation("✅ [GetRooms] Returning {Count} rooms with player counts", result.Count);

        return Ok(result);
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

        var result = rooms.Select(r => new RoomListItemDto
        {
            Id = r.Id,
            Name = r.Name,
            OwnerUsername = r.Owner.Username,
            IsPrivate = r.Visibility == RoomVisibility.Private,
            MaxPlayers = r.MaxPlayers,
            CurrentPlayers = 0, // TODO: подсчитать из активной сессии
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        }).ToList();

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

        _logger.LogInformation("🔍 [GetRoom] Loading room {RoomId}", id);
        _logger.LogInformation("🔍 [GetRoom] Room owner: {OwnerId}", room.OwnerUserId);
        
        // Получаем активную сессию для загрузки игроков
        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);
        
        _logger.LogInformation("🔍 [GetRoom] GetActiveByRoomIdAsync returned: {Result}", session != null ? $"Session {session.Id}" : "NULL");
        
        var players = new List<PlayerInRoomDto>();
        if (session != null)
        {
            _logger.LogInformation("📊 [GetRoom] Loading players from session {SessionId}", session.Id);
            _logger.LogInformation("📊 [GetRoom] Session has {Count} players", session.Players.Count);
            
            foreach (var player in session.Players.Where(p => p.IsActive))
            {
                _logger.LogInformation("👤 [GetRoom] Processing player {UserId}, IsActive: {IsActive}", player.UserId, player.IsActive);
                
                var user = await _unitOfWork.Users.GetByIdAsync(player.UserId);
                if (user != null)
                {
                    var isRoomOwner = player.UserId == room.OwnerUserId;
                    
                    _logger.LogInformation("✅ [GetRoom] Adding player: {Username}, IsOwner: {IsOwner}", user.Username, isRoomOwner);
                    
                    players.Add(new PlayerInRoomDto
                    {
                        UserId = player.UserId,
                        Username = user.Username,
                        IsOwner = isRoomOwner,
                        IsReady = false,
                        JoinedAt = player.JoinedAt
                    });
                }
                else
                {
                    _logger.LogWarning("❌ [GetRoom] User {UserId} not found!", player.UserId);
                }
            }
            
            _logger.LogInformation("✅ [GetRoom] Loaded {Count} players for room {RoomId}", players.Count, id);
        }
        else
        {
            _logger.LogWarning("❌ [GetRoom] No active session found for room {RoomId}!", id);
            _logger.LogWarning("❌ [GetRoom] This means session was not created or not found!");
        }
        
        var result = new RoomDetailsDto
        {
            Id = room.Id,
            OwnerId = room.OwnerUserId,
            OwnerUsername = room.Owner.Username,
            Name = room.Name,
            IsPrivate = room.Visibility == RoomVisibility.Private,
            MaxPlayers = room.MaxPlayers,
            Status = room.Status.ToString(),
            VictoryConditionType = room.VictoryConditionType.ToString(),
            VictoryValue = room.VictoryValue,
            CreatedAt = room.CreatedAt,
            Players = players,
            CurrentSessionId = session?.Id
        };

        return Ok(result);
    }

    /// <summary>
    /// Создать новую комнату
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Room name is required");

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            // Парсим строки в enum'ы
            if (!Enum.TryParse<RoomVisibility>(request.Visibility, true, out var visibility))
                visibility = RoomVisibility.Public;

            if (!Enum.TryParse<VictoryConditionType>(request.VictoryConditionType, true, out var victoryType))
                victoryType = VictoryConditionType.Points;

            if (!Enum.TryParse<TagSelectionMode>(request.TagSelectionMode, true, out var tagMode))
                tagMode = TagSelectionMode.Any;

            string? accessCodeHash = null;
            if (visibility == RoomVisibility.Private && !string.IsNullOrWhiteSpace(request.AccessCode))
            {
                accessCodeHash = _passwordHasher.HashPassword(request.AccessCode);
            }

            var room = new Room(
                userId.Value,
                request.Name,
                visibility,
                request.MaxPlayers,
                victoryType,
                request.VictoryValue,
                tagMode);

            if (accessCodeHash != null)
            {
                room.SetAccessCode(accessCodeHash);
            }

            // Добавляем выбранные теги
            foreach (var tagSelection in request.TagSelections)
            {
                room.AddTagSelection(tagSelection.TagId, tagSelection.Weight);
            }

            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("🏠 [Room] Room {RoomId} created by user {UserId}", room.Id, userId);

            // Загружаем пользователя для получения username
            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
            
            // ВАЖНО: Создаем GameSession автоматически и добавляем создателя как игрока
            // Используем количество раундов из запроса (по умолчанию 10)
            var numberOfRounds = request.NumberOfRounds > 0 ? request.NumberOfRounds : 10;
            var session = new GameSession(room.Id, numberOfRounds);
            
            _logger.LogInformation("📊 [CreateRoom] Game session created with {Rounds} rounds", numberOfRounds);
            session.AddPlayer(userId.Value, true); // isOwner = true
            await _unitOfWork.GameSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("👤 [Room] Creator {Username} automatically joined room {RoomId} as player in session {SessionId}", 
                user?.Username, room.Id, session.Id);
            
            // Возвращаем RoomListItemDto для совместимости с Frontend
            var result = new RoomListItemDto
            {
                Id = room.Id,
                Name = room.Name,
                OwnerUsername = user?.Username ?? "Unknown",
                IsPrivate = room.Visibility == RoomVisibility.Private,
                MaxPlayers = room.MaxPlayers,
                CurrentPlayers = 1, // Создатель комнаты
                Status = room.Status.ToString(),
                CreatedAt = room.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetRoom),
                new { id = room.Id },
                result);
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

        _logger.LogInformation("🔍 [JoinRoom] Step 3: Getting current user...");
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            _logger.LogError("❌ [JoinRoom] User ID is NULL - Unauthorized!");
            return Unauthorized();
        }
        _logger.LogInformation("✅ [JoinRoom] User ID: {UserId}", userId.Value);

        // Получаем или создаем активную сессию
        _logger.LogInformation("🔍 [JoinRoom] Step 4: Loading active session for room...");
        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
        {
            _logger.LogWarning("⚠️ [JoinRoom] No active session found - creating new one...");
            // Создаем новую сессию если её нет
            // Количество раундов по умолчанию 10 (если сессия создается при старте игры)
            session = new GameSession(id, 10);
            _logger.LogInformation("📊 [StartGame] Game session created with default 10 rounds");
            await _unitOfWork.GameSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("✅ [JoinRoom] New session created: {SessionId}", session.Id);
        }
        else
        {
            _logger.LogInformation("✅ [JoinRoom] Active session found: {SessionId}, Players: {Count}", 
                session.Id, session.Players.Count);
        }

        // Проверяем, не присоединился ли уже игрок
        _logger.LogInformation("🔍 [JoinRoom] Step 5: Checking if player already in session...");
        var existingPlayer = session.Players.FirstOrDefault(p => p.UserId == userId.Value && p.IsActive);
        if (existingPlayer != null)
        {
            _logger.LogInformation("ℹ️ [JoinRoom] User {UserId} ALREADY in session!", userId);
            return Ok(new { success = true, message = "Already in room", sessionId = session.Id });
        }
        _logger.LogInformation("✅ [JoinRoom] Player not in session - can join");

        // Проверяем лимит игроков
        _logger.LogInformation("🔍 [JoinRoom] Step 6: Checking room capacity...");
        var activePlayers = session.Players.Count(p => p.IsActive);
        _logger.LogInformation("📊 [JoinRoom] Current players: {Count}/{Max}", activePlayers, room.MaxPlayers);
        
        if (activePlayers >= room.MaxPlayers)
        {
            _logger.LogError("❌ [JoinRoom] Room is FULL! ({Count}/{Max})", activePlayers, room.MaxPlayers);
            return BadRequest($"Room is full ({activePlayers}/{room.MaxPlayers})");
        }
        _logger.LogInformation("✅ [JoinRoom] Room has space");

        // Добавляем игрока в сессию
        _logger.LogInformation("🔍 [JoinRoom] Step 7: Adding player to session...");
        var isOwner = room.OwnerUserId == userId.Value;
        _logger.LogInformation("📊 [JoinRoom] IsOwner: {IsOwner}", isOwner);
        
        session.AddPlayer(userId.Value, isOwner);
        _logger.LogInformation("✅ [JoinRoom] session.AddPlayer() called");
        
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("✅ [JoinRoom] Changes saved to database");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        _logger.LogInformation("🎉 [JoinRoom] ========== JOIN SUCCESS ==========");
        _logger.LogInformation("✅ [JoinRoom] User {Username} (ID: {UserId}) joined room {RoomId}", 
            user?.Username ?? "Unknown", userId, room.Name);
        _logger.LogInformation("✅ [JoinRoom] Session: {SessionId}, Total players: {Count}/{Max}", 
            session.Id, activePlayers + 1, room.MaxPlayers);
        
        // 🔥 КРИТИЧНО: Отправляем уведомление через SignalR всем игрокам в комнате
        var groupName = $"Room_{id}";
        _logger.LogInformation("📡 [JoinRoom] Sending PlayerJoined notification to group: {GroupName}", groupName);
        
        try
        {
            await _lobbyHubContext.Clients.Group(groupName).SendAsync("PlayerJoined", new
            {
                userId = userId.Value,
                username = user?.Username ?? "Unknown",
                isOwner = isOwner,
                joinedAt = DateTime.UtcNow,
                playersCount = activePlayers + 1
            });
            
            _logger.LogInformation("✅ [JoinRoom] PlayerJoined notification sent successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [JoinRoom] Failed to send PlayerJoined notification via SignalR");
        }
        
        _logger.LogInformation("🎉 [JoinRoom] ========== JOIN END ==========");

        return Ok(new
        {
            success = true,
            sessionId = session.Id,
            playersCount = activePlayers + 1
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
        _logger.LogInformation("🎮 [StartGame] ========== START GAME REQUEST ==========");
        _logger.LogInformation("🎮 [StartGame] Room ID: {RoomId}", id);
        
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
        {
            _logger.LogError("❌ [StartGame] Room not found: {RoomId}", id);
            return NotFound($"Room with ID {id} not found");
        }

        _logger.LogInformation("✅ [StartGame] Room found: {RoomName}, Status: {Status}", room.Name, room.Status);

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            _logger.LogError("❌ [StartGame] User ID is NULL - Unauthorized!");
            return Unauthorized();
        }

        _logger.LogInformation("✅ [StartGame] User ID: {UserId}", userId.Value);

        // Проверка прав владельца
        if (room.OwnerUserId != userId.Value)
        {
            _logger.LogError("❌ [StartGame] User {UserId} is NOT owner! Owner: {OwnerId}", userId, room.OwnerUserId);
            return Forbid();
        }

        _logger.LogInformation("✅ [StartGame] User is owner - can start game");

        if (room.Status != RoomStatus.Lobby)
        {
            _logger.LogError("❌ [StartGame] Room status is {Status}, not Lobby!", room.Status);
            return BadRequest($"Game can only be started from lobby. Current status: {room.Status}");
        }

        _logger.LogInformation("✅ [StartGame] Room status is Lobby");

        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
        {
            _logger.LogError("❌ [StartGame] No active session found for room {RoomId}", id);
            return BadRequest("No active session found");
        }

        _logger.LogInformation("✅ [StartGame] Session found: {SessionId}, Status: {Status}", session.Id, session.Status);

        var activePlayers = session.Players.Count(p => p.IsActive);
        _logger.LogInformation("📊 [StartGame] Active players: {Count}", activePlayers);
        
        if (activePlayers < 2)
        {
            _logger.LogError("❌ [StartGame] Not enough players: {Count}/2", activePlayers);
            return BadRequest($"At least 2 players are required to start the game. Current: {activePlayers}");
        }

        _logger.LogInformation("✅ [StartGame] Enough players to start");

        try
        {
            _logger.LogInformation("🔄 [StartGame] Calling room.StartGame()...");
            room.StartGame();
            _logger.LogInformation("✅ [StartGame] room.StartGame() succeeded. New status: {Status}", room.Status);

            _logger.LogInformation("🔄 [StartGame] Calling session.Start()...");
            _logger.LogInformation("   Session current status: {Status}", session.Status);
            session.Start();
            _logger.LogInformation("✅ [StartGame] session.Start() succeeded. New status: {Status}", session.Status);

            // 🎯 КРИТИЧНО: Автоматически создаем и запускаем первый раунд!
            _logger.LogInformation("🎲 [StartGame] Creating first round...");
            
            // Получаем случайный вопрос по тегам комнаты
            var tagIds = room.TagSelections.Select(ts => ts.TagId).ToArray();
            var question = await _unitOfWork.Questions.GetRandomApprovedAsync(tagIds.Any() ? tagIds : null);
            
            if (question == null)
            {
                _logger.LogError("❌ [StartGame] No approved questions available!");
                return BadRequest("No approved questions available for this game");
            }
            
            _logger.LogInformation("✅ [StartGame] Question selected: {QuestionId} - '{QuestionText}'", 
                question.Id, question.PromptText);
            
            // Создаем первый раунд
            var timeLimitSec = 60; // TODO: взять из настроек комнаты
            var firstRound = session.AddRound(question.Id, timeLimitSec);
            session.StartRound(firstRound.Id);
            
            _logger.LogInformation("✅ [StartGame] First round created: {RoundId}, Time limit: {Time}s", 
                firstRound.Id, timeLimitSec);

            _logger.LogInformation("💾 [StartGame] Saving changes...");
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("✅ [StartGame] Changes saved!");

            _logger.LogInformation("🎉 [StartGame] ========== GAME STARTED SUCCESSFULLY ==========");
            _logger.LogInformation("   Room: {RoomId}, Session: {SessionId}", id, session.Id);
            _logger.LogInformation("   Players: {Count}", activePlayers);
            _logger.LogInformation("   First Round: {RoundId}, Question: {QuestionId}", firstRound.Id, question.Id);

            // 🔥 КРИТИЧНО: Отправляем уведомление GameStarted через SignalR
            var groupName = $"Room_{id}";
            _logger.LogInformation("📡 [StartGame] Sending GameStarted notification to group: {GroupName}", groupName);
            
            try
            {
                await _lobbyHubContext.Clients.Group(groupName).SendAsync("GameStarted", session.Id);
                _logger.LogInformation("✅ [StartGame] GameStarted notification sent successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [StartGame] Failed to send GameStarted notification via SignalR");
            }

            return Ok(new
            {
                message = "Game started",
                sessionId = session.Id,
                roomStatus = room.Status.ToString(),
                sessionStatus = session.Status.ToString(),
                firstRoundId = firstRound.Id,
                currentQuestionId = question.Id
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "❌ [StartGame] InvalidOperationException: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [StartGame] Unexpected exception: {Message}", ex.Message);
            return BadRequest($"Error starting game: {ex.Message}");
        }
    }

    /// <summary>
    /// Вернуть комнату в лобби (для отладки и рестарта)
    /// </summary>
    [HttpPost("{id}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetRoom(Guid id)
    {
        _logger.LogInformation("🔄 [ResetRoom] Resetting room {RoomId} to Lobby", id);
        
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
            return NotFound($"Room with ID {id} not found");

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        // Проверка прав владельца
        if (room.OwnerUserId != userId.Value)
        {
            _logger.LogError("❌ [ResetRoom] User {UserId} is not owner", userId);
            return Forbid();
        }

        try
        {
            _logger.LogInformation("🔄 [ResetRoom] Current status: {Status}", room.Status);
            room.ReturnToLobby();
            _logger.LogInformation("✅ [ResetRoom] New status: {Status}", room.Status);
            
            // Также сбрасываем сессию если нужно
            var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);
            if (session != null && session.Status != GameSessionStatus.Pending)
            {
                _logger.LogInformation("🔄 [ResetRoom] Aborting active session {SessionId}", session.Id);
                session.Abort();
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("✅ [ResetRoom] Room reset successfully");

            return Ok(new
            {
                message = "Room reset to lobby",
                roomStatus = room.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ResetRoom] Error: {Message}", ex.Message);
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
