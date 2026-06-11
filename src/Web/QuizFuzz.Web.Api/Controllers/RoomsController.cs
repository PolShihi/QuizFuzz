using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Rooms;
using QuizFuzz.Web.Api.Hubs;
using QuizFuzz.Web.Api.Services;

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
    private readonly IHintRevealScheduler _hintRevealScheduler;
    private readonly IRoomCleanupService _roomCleanupService;

    public RoomsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        ILogger<RoomsController> logger,
        IHubContext<LobbyHub> lobbyHubContext,
        IHintRevealScheduler hintRevealScheduler,
        IRoomCleanupService roomCleanupService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _lobbyHubContext = lobbyHubContext;
        _hintRevealScheduler = hintRevealScheduler;
        _roomCleanupService = roomCleanupService;
    }

    /// <summary>
    /// Получить список всех комнат (публичных и приватных для авторизованных)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms()
    {
        _logger.LogDebug(" [GetRooms] Loading lobby rooms list");
        
        await _roomCleanupService.CleanupAsync(HttpContext.RequestAborted);
        var rooms = await _unitOfWork.Rooms.GetLobbyRoomsAsync(HttpContext.RequestAborted);
        
        _logger.LogDebug(" [GetRooms] Found {Count} rooms", rooms.Count());

        var result = new List<RoomListItemDto>();
        
        foreach (var r in rooms)
        {
            // Получаем активную сессию для подсчета игроков
            var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(r.Id);
            var playerCount = session?.Players.Count(p => p.IsActive) ?? 0;
            
            _logger.LogDebug(" [GetRooms] Room '{RoomName}': {Players}/{MaxPlayers} players", 
                r.Name, playerCount, r.MaxPlayers);
            
            result.Add(new RoomListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                OwnerUsername = r.Owner.Username,
                IsPrivate = r.Visibility == RoomVisibility.Private,
                MaxPlayers = r.MaxPlayers,
                CurrentPlayers = playerCount, //  Реальное количество!
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            });
        }
        
        _logger.LogDebug(" [GetRooms] Returning {Count} rooms with player counts", result.Count);

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
        await _roomCleanupService.CleanupAsync(HttpContext.RequestAborted);
        var rooms = await _unitOfWork.Rooms.GetPublicRoomsAsync(HttpContext.RequestAborted);

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

        _logger.LogDebug(" [GetRoom] Loading room {RoomId}", id);
        _logger.LogDebug(" [GetRoom] Room owner: {OwnerId}", room.OwnerUserId);
        
        // Получаем активную сессию для загрузки игроков
        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);
        
        _logger.LogDebug(" [GetRoom] GetActiveByRoomIdAsync returned: {Result}", session != null ? $"Session {session.Id}" : "NULL");
        
        var players = new List<PlayerInRoomDto>();
        if (session != null)
        {
            _logger.LogDebug(" [GetRoom] Loading players from session {SessionId}", session.Id);
            _logger.LogDebug(" [GetRoom] Session has {Count} players", session.Players.Count);
            
            var activePlayers = session.Players.Where(p => p.IsActive).ToList();
            var activeSubscriptions = await _unitOfWork.UserSubscriptions.GetActiveByUserIdsAsync(
                activePlayers.Select(p => p.UserId),
                DateTime.UtcNow);
            var activeSubscriptionByUserId = activeSubscriptions
                .GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.ExpiresAt).First());

            foreach (var player in activePlayers)
            {
                _logger.LogDebug(" [GetRoom] Processing player {UserId}, IsActive: {IsActive}", player.UserId, player.IsActive);
                
                var user = await _unitOfWork.Users.GetByIdAsync(player.UserId);
                if (user != null)
                {
                    var isRoomOwner = player.UserId == room.OwnerUserId;
                    
                    _logger.LogDebug(" [GetRoom] Adding player: {Username}, IsOwner: {IsOwner}", user.Username, isRoomOwner);
                    
                    players.Add(new PlayerInRoomDto
                    {
                        UserId = player.UserId,
                        Username = user.Username,
                        IsOwner = isRoomOwner,
                        IsReady = false,
                        JoinedAt = player.JoinedAt,
                        HasActiveSubscription = activeSubscriptionByUserId.ContainsKey(player.UserId),
                        SubscriptionExpiresAt = activeSubscriptionByUserId.TryGetValue(player.UserId, out var playerSubscription)
                            ? playerSubscription.ExpiresAt
                            : null
                    });
                }
                else
                {
                    _logger.LogDebug(" [GetRoom] User {UserId} not found!", player.UserId);
                }
            }
            
            _logger.LogDebug(" [GetRoom] Loaded {Count} players for room {RoomId}", players.Count, id);
        }
        else
        {
            _logger.LogDebug(" [GetRoom] No active session found for room {RoomId}!", id);
            _logger.LogDebug(" [GetRoom] This means session was not created or not found!");
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
            if (visibility == RoomVisibility.Private)
            {
                var accessCode = request.AccessCode?.Trim();
                if (string.IsNullOrWhiteSpace(accessCode))
                    return BadRequest("Access code is required for private rooms");

                if (accessCode.Length < 4 || accessCode.Length > 32)
                    return BadRequest("Access code must be between 4 and 32 characters");

                accessCodeHash = _passwordHasher.HashPassword(accessCode);
            }
            
            // Сериализуем фильтры сложности в JSON
            string? difficultyFiltersJson = null;
            if (request.DifficultyFilters != null && request.DifficultyFilters.Any())
            {
                difficultyFiltersJson = System.Text.Json.JsonSerializer.Serialize(request.DifficultyFilters);
                _logger.LogDebug(" [CreateRoom] Difficulty filters: {Filters}", difficultyFiltersJson);
            }
            
            // Определяем VictoryValue в зависимости от режима
            int victoryValue;
            if (victoryType == VictoryConditionType.Points)
            {
                victoryValue = request.VictoryValue;
                _logger.LogDebug(" [CreateRoom] Victory Mode: POINTS, Target: {Value} points", victoryValue);
            }
            else // Questions
            {
                victoryValue = request.NumberOfRounds;
                _logger.LogDebug(" [CreateRoom] Victory Mode: QUESTIONS, Target: {Value} questions", victoryValue);
            }

            var room = new Room(
                userId.Value,
                request.Name,
                visibility,
                request.MaxPlayers,
                victoryType,
                victoryValue,
                tagMode,
                difficultyFiltersJson);

            if (accessCodeHash != null)
            {
                room.SetAccessCode(accessCodeHash);
            }

            // Добавляем выбранные теги
            _logger.LogDebug(" [CreateRoom] Adding {Count} tag selections", request.TagSelections.Count);
            foreach (var tagSelection in request.TagSelections)
            {
                room.AddTagSelection(tagSelection.TagId, tagSelection.Weight);
                _logger.LogDebug("    Tag {TagId} added with weight {Weight}", tagSelection.TagId, tagSelection.Weight);
            }

            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug(" [Room] Room {RoomId} created by user {UserId}", room.Id, userId);

            // Загружаем пользователя для получения username
            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
            
            // ВАЖНО: Создаем GameSession автоматически и добавляем создателя как игрока
            // Для режима Questions используем victoryValue, иначе большое число
            var numberOfRounds = (victoryType == VictoryConditionType.Questions) 
                ? victoryValue 
                : 999; // Для режима Points ставим большое число, т.к. игра закончится по очкам
            
            var session = new GameSession(room.Id, numberOfRounds);
            
            _logger.LogDebug(" [CreateRoom] Game session created");
            _logger.LogDebug("   Victory Type: {VictoryType}", victoryType);
            _logger.LogDebug("   Victory Value: {VictoryValue}", victoryValue);
            _logger.LogDebug("   Session Rounds Planned: {Rounds}", numberOfRounds);
            session.AddPlayer(userId.Value, true); // isOwner = true
            await _unitOfWork.GameSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogDebug(" [Room] Creator {Username} automatically joined room {RoomId} as player in session {SessionId}", 
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
            _logger.LogDebug(ex, "Error creating room");
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
        if (room.Visibility == RoomVisibility.Private)
        {
            if (string.IsNullOrWhiteSpace(room.AccessCodeHash))
                return BadRequest("Private room is missing an access code. Please recreate the room.");

            var accessCode = request?.AccessCode?.Trim();
            if (string.IsNullOrWhiteSpace(accessCode))
                return BadRequest("Access code is required for private rooms");

            if (!_passwordHasher.VerifyPassword(accessCode, room.AccessCodeHash))
                return BadRequest("Invalid access code");
        }

        _logger.LogDebug(" [JoinRoom] Step 3: Getting current user...");
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            _logger.LogDebug(" [JoinRoom] User ID is NULL - Unauthorized!");
            return Unauthorized();
        }
        _logger.LogDebug(" [JoinRoom] User ID: {UserId}", userId.Value);

        // Получаем или создаем активную сессию
        _logger.LogDebug(" [JoinRoom] Step 4: Loading active session for room...");
        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
        {
            _logger.LogDebug(" [JoinRoom] No active session found - creating new one...");
            // Создаем новую сессию если её нет
            // Количество раундов по умолчанию 10 (если сессия создается при старте игры)
            session = new GameSession(id, 10);
            _logger.LogDebug(" [StartGame] Game session created with default 10 rounds");
            await _unitOfWork.GameSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug(" [JoinRoom] New session created: {SessionId}", session.Id);
        }
        else
        {
            _logger.LogDebug(" [JoinRoom] Active session found: {SessionId}, Players: {Count}", 
                session.Id, session.Players.Count);
        }

        // Проверяем, не присоединился ли уже игрок
        _logger.LogDebug(" [JoinRoom] Step 5: Checking if player already in session...");
        var existingPlayer = session.Players.FirstOrDefault(p => p.UserId == userId.Value && p.IsActive);
        if (existingPlayer != null)
        {
            _logger.LogDebug("ℹ [JoinRoom] User {UserId} ALREADY in session!", userId);
            return Ok(new { success = true, message = "Already in room", sessionId = session.Id });
        }
        _logger.LogDebug(" [JoinRoom] Player not in session - can join");

        // Проверяем лимит игроков
        _logger.LogDebug(" [JoinRoom] Step 6: Checking room capacity...");
        var activePlayers = session.Players.Count(p => p.IsActive);
        _logger.LogDebug(" [JoinRoom] Current players: {Count}/{Max}", activePlayers, room.MaxPlayers);
        
        if (activePlayers >= room.MaxPlayers)
        {
            _logger.LogDebug(" [JoinRoom] Room is FULL! ({Count}/{Max})", activePlayers, room.MaxPlayers);
            return BadRequest($"Room is full ({activePlayers}/{room.MaxPlayers})");
        }
        _logger.LogDebug(" [JoinRoom] Room has space");

        // Добавляем игрока в сессию
        _logger.LogDebug(" [JoinRoom] Step 7: Adding player to session...");
        var isOwner = room.OwnerUserId == userId.Value;
        _logger.LogDebug(" [JoinRoom] IsOwner: {IsOwner}", isOwner);
        
        session.AddPlayer(userId.Value, isOwner);
        _logger.LogDebug(" [JoinRoom] session.AddPlayer() called");
        
        await _unitOfWork.SaveChangesAsync();
        _logger.LogDebug(" [JoinRoom] Changes saved to database");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        _logger.LogDebug(" [JoinRoom] ========== JOIN SUCCESS ==========");
        _logger.LogDebug(" [JoinRoom] User {Username} (ID: {UserId}) joined room {RoomId}", 
            user?.Username ?? "Unknown", userId, room.Name);
        _logger.LogDebug(" [JoinRoom] Session: {SessionId}, Total players: {Count}/{Max}", 
            session.Id, activePlayers + 1, room.MaxPlayers);
        
        //  КРИТИЧНО: Отправляем уведомление через SignalR всем игрокам в комнате
        var groupName = $"Room_{id}";
        _logger.LogDebug(" [JoinRoom] Sending PlayerJoined notification to group: {GroupName}", groupName);
        
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
            
            _logger.LogDebug(" [JoinRoom] PlayerJoined notification sent successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [JoinRoom] Failed to send PlayerJoined notification via SignalR");
        }
        
        _logger.LogDebug(" [JoinRoom] ========== JOIN END ==========");

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

        _logger.LogDebug("User {UserId} left room {RoomId}", userId, id);

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
        _logger.LogDebug(" [StartGame] ========== START GAME REQUEST ==========");
        _logger.LogDebug(" [StartGame] Room ID: {RoomId}", id);
        
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
        {
            _logger.LogDebug(" [StartGame] Room not found: {RoomId}", id);
            return NotFound($"Room with ID {id} not found");
        }

        _logger.LogDebug(" [StartGame] Room found: {RoomName}, Status: {Status}", room.Name, room.Status);

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            _logger.LogDebug(" [StartGame] User ID is NULL - Unauthorized!");
            return Unauthorized();
        }

        _logger.LogDebug(" [StartGame] User ID: {UserId}", userId.Value);

        // Проверка прав владельца
        if (room.OwnerUserId != userId.Value)
        {
            _logger.LogDebug(" [StartGame] User {UserId} is NOT owner! Owner: {OwnerId}", userId, room.OwnerUserId);
            return Forbid();
        }

        _logger.LogDebug(" [StartGame] User is owner - can start game");

        if (room.Status != RoomStatus.Lobby)
        {
            _logger.LogDebug(" [StartGame] Room status is {Status}, not Lobby!", room.Status);
            return BadRequest($"Game can only be started from lobby. Current status: {room.Status}");
        }

        _logger.LogDebug(" [StartGame] Room status is Lobby");

        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);

        if (session == null)
        {
            _logger.LogDebug(" [StartGame] No active session found for room {RoomId}", id);
            return BadRequest("No active session found");
        }

        _logger.LogDebug(" [StartGame] Session found: {SessionId}, Status: {Status}", session.Id, session.Status);

        var activePlayers = session.Players.Count(p => p.IsActive);
        _logger.LogDebug(" [StartGame] Active players: {Count}", activePlayers);
        
        if (activePlayers < 2)
        {
            _logger.LogDebug(" [StartGame] Not enough players: {Count}/2", activePlayers);
            return BadRequest($"At least 2 players are required to start the game. Current: {activePlayers}");
        }

        _logger.LogDebug(" [StartGame] Enough players to start");

        try
        {
            _logger.LogDebug(" [StartGame] Calling room.StartGame()...");
            room.StartGame();
            _logger.LogDebug(" [StartGame] room.StartGame() succeeded. New status: {Status}", room.Status);

            _logger.LogDebug(" [StartGame] Calling session.Start()...");
            _logger.LogDebug("   Session current status: {Status}", session.Status);
            session.Start();
            _logger.LogDebug(" [StartGame] session.Start() succeeded. New status: {Status}", session.Status);

            //  КРИТИЧНО: Автоматически создаем и запускаем первый раунд!
            _logger.LogDebug(" [StartGame] Creating first round...");
            
            // Получаем случайный вопрос по тегам и сложности комнаты
            var tagIds = room.TagSelections.Select(ts => ts.TagId).ToArray();
            var difficultyFilters = room.GetDifficultyFilters();
            
            _logger.LogDebug(" [StartGame] Selecting first question with filters:");
            _logger.LogDebug("    Tags: {TagCount}", tagIds.Length);
            _logger.LogDebug("    Difficulties: {Filters}", 
                difficultyFilters.Any() ? string.Join(", ", difficultyFilters) : "ALL");
            
            var question = await _unitOfWork.Questions.GetRandomApprovedWithFiltersAsync(
                tagIds.Any() ? tagIds : null,
                difficultyFilters.Any() ? difficultyFilters : null);
            
            if (question == null)
            {
                _logger.LogDebug(" [StartGame] No approved questions available with specified filters!");
                _logger.LogDebug("   Tags: {Tags}", tagIds.Any() ? string.Join(", ", tagIds) : "None");
                _logger.LogDebug("   Difficulties: {Diff}", difficultyFilters.Any() ? string.Join(", ", difficultyFilters) : "None");
                return BadRequest("No approved questions available with specified filters");
            }
            
            _logger.LogDebug(" [StartGame] Question selected: {QuestionId}, Difficulty: {Difficulty} - '{QuestionText}'", 
                question.Id, question.Difficulty, question.PromptText);
            
            // Создаем первый раунд
            var timeLimitSec = 60; // TODO: взять из настроек комнаты
            var firstRound = session.AddRound(question.Id, timeLimitSec);
            session.StartRound(firstRound.Id);
            
            _logger.LogDebug(" [StartGame] First round created: {RoundId}, Time limit: {Time}s", 
                firstRound.Id, timeLimitSec);

            _logger.LogDebug(" [StartGame] Saving changes...");
            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug(" [StartGame] Changes saved!");

            _hintRevealScheduler.ScheduleHints(room.Id, firstRound.Id);
            _logger.LogDebug(" [StartGame] Hint reveal scheduler started for first round {RoundId}", firstRound.Id);

            _logger.LogDebug(" [StartGame] ========== GAME STARTED SUCCESSFULLY ==========");
            _logger.LogDebug("   Room: {RoomId}, Session: {SessionId}", id, session.Id);
            _logger.LogDebug("   Players: {Count}", activePlayers);
            _logger.LogDebug("   First Round: {RoundId}, Question: {QuestionId}", firstRound.Id, question.Id);

            //  КРИТИЧНО: Отправляем уведомление GameStarted через SignalR
            var groupName = $"Room_{id}";
            _logger.LogDebug(" [StartGame] Sending GameStarted notification to group: {GroupName}", groupName);
            
            try
            {
                await _lobbyHubContext.Clients.Group(groupName).SendAsync("GameStarted", session.Id);
                _logger.LogDebug(" [StartGame] GameStarted notification sent successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, " [StartGame] Failed to send GameStarted notification via SignalR");
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
            _logger.LogDebug(ex, " [StartGame] InvalidOperationException: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [StartGame] Unexpected exception: {Message}", ex.Message);
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
        _logger.LogDebug(" [ResetRoom] Resetting room {RoomId} to Lobby", id);
        
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(id);

        if (room == null)
            return NotFound($"Room with ID {id} not found");

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        // Проверка прав владельца
        if (room.OwnerUserId != userId.Value)
        {
            _logger.LogDebug(" [ResetRoom] User {UserId} is not owner", userId);
            return Forbid();
        }

        try
        {
            _logger.LogDebug(" [ResetRoom] Current status: {Status}", room.Status);
            room.ReturnToLobby();
            _logger.LogDebug(" [ResetRoom] New status: {Status}", room.Status);
            
            // Также сбрасываем сессию если нужно
            var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(id);
            if (session != null && session.Status != GameSessionStatus.Pending)
            {
                _logger.LogDebug(" [ResetRoom] Aborting active session {SessionId}", session.Id);
                session.Abort();
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogDebug(" [ResetRoom] Room reset successfully");

            return Ok(new
            {
                message = "Room reset to lobby",
                roomStatus = room.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [ResetRoom] Error: {Message}", ex.Message);
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

            _logger.LogDebug("Room {RoomId} updated by user {UserId}", id, userId);

            return Ok(new { message = "Room updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating room {RoomId}", id);
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
