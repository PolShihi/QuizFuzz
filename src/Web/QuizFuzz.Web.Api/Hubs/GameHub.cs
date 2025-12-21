using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Web.Api.Hubs;

/// <summary>
/// SignalR Hub для реалтайм игрового процесса
/// </summary>
[Authorize]
public class GameHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFuzzyMatchingService _fuzzyMatchingService;
    private readonly ILogger<GameHub> _logger;

    // Group names format: "room:{roomId}"
    private const string RoomGroupPrefix = "room:";
    
    public GameHub(
        IUnitOfWork unitOfWork,
        IFuzzyMatchingService fuzzyMatchingService,
        ILogger<GameHub> logger)
    {
        _unitOfWork = unitOfWork;
        _fuzzyMatchingService = fuzzyMatchingService;
        _logger = logger;
    }

    /// <summary>
    /// Присоединиться к игровой сессии (используется при старте игры)
    /// </summary>
    public async Task JoinGame(string sessionId)
    {
        _logger.LogInformation("🎮 [GameHub] ========== JOIN GAME REQUEST ==========");
        _logger.LogInformation("🎮 [GameHub] SessionId: {SessionId}", sessionId);
        _logger.LogInformation("🎮 [GameHub] ConnectionId: {ConnectionId}", Context.ConnectionId);
        
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            _logger.LogError("❌ [GameHub] Invalid SessionId format: {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "Invalid session ID format");
            return;
        }

        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Unknown";
        
        _logger.LogInformation("✅ [GameHub] UserId: {UserId}, Username: {Username}", userId, username);

        var session = await _unitOfWork.GameSessions.GetWithDetailsAsync(parsedSessionId);
        if (session == null)
        {
            _logger.LogError("❌ [GameHub] Session not found: {SessionId}", parsedSessionId);
            await Clients.Caller.SendAsync("Error", "Session not found");
            return;
        }

        _logger.LogInformation("✅ [GameHub] Session found: {SessionId}, Status: {Status}", 
            parsedSessionId, session.Status);

        var room = session.Room;
        _logger.LogInformation("✅ [GameHub] Room: {RoomId}, Name: {RoomName}", room.Id, room.Name);

        // Проверяем, что игрок в сессии
        var player = session.Players.FirstOrDefault(p => p.UserId == userId && p.IsActive);
        if (player == null)
        {
            _logger.LogError("❌ [GameHub] User {UserId} is not a player in session {SessionId}", 
                userId, parsedSessionId);
            await Clients.Caller.SendAsync("Error", "You are not a player in this session");
            return;
        }

        _logger.LogInformation("✅ [GameHub] Player found in session");

        var groupName = GetRoomGroupName(room.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("✅ [GameHub] Added to group: {GroupName}", groupName);

        // Уведомляем всех в комнате
        await Clients.Group(groupName).SendAsync("PlayerJoined", new
        {
            userId,
            username,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogInformation("✅ [GameHub] Sent PlayerJoined to group");

        // Отправляем текущее состояние игры присоединившемуся
        await Clients.Caller.SendAsync("GameState", new
        {
            sessionId = session.Id,
            roomId = room.Id,
            roomName = room.Name,
            roomStatus = room.Status.ToString(),
            sessionStatus = session.Status.ToString(),
            currentRoundId = session.CurrentRoundId,
            totalRoundsPlanned = session.TotalRoundsPlanned,
            totalRoundsPlayed = session.TotalRoundsPlayed,
            players = session.Players
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    userId = p.UserId,
                    username = p.User.Username,
                    isOwner = p.IsOwnerSnapshot
                })
                .ToList()
        });
        
        _logger.LogInformation("✅ [GameHub] Sent GameState to caller");
        _logger.LogInformation("🎉 [GameHub] ========== JOIN GAME SUCCESS ==========");
    }

    /// <summary>
    /// Присоединиться к игровой комнате (используется в лобби)
    /// </summary>
    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Unknown";

        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(roomId);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found");
            return;
        }

        var session = await _unitOfWork.GameSessions.GetActiveByRoomIdAsync(roomId);
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "No active session found");
            return;
        }

        // Проверяем, что игрок в сессии
        var player = session.Players.FirstOrDefault(p => p.UserId == userId && p.IsActive);
        if (player == null)
        {
            await Clients.Caller.SendAsync("Error", "You are not a player in this room");
            return;
        }

        var groupName = GetRoomGroupName(roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} ({Username}) joined room {RoomId} via SignalR",
            userId, username, roomId);

        // Уведомляем всех в комнате
        await Clients.Group(groupName).SendAsync("PlayerJoined", new
        {
            userId,
            username,
            timestamp = DateTime.UtcNow
        });

        // Отправляем текущее состояние комнаты присоединившемуся
        await Clients.Caller.SendAsync("RoomState", new
        {
            roomId,
            roomName = room.Name,
            status = room.Status.ToString(),
            sessionId = session.Id,
            sessionStatus = session.Status.ToString(),
            currentRoundId = session.CurrentRoundId,
            players = session.Players
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    userId = p.UserId,
                    username = p.User.Username,
                    isOwner = p.IsOwnerSnapshot
                })
        });
    }

    /// <summary>
    /// Покинуть игровую комнату
    /// </summary>
    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Unknown";

        var groupName = GetRoomGroupName(roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} ({Username}) left room {RoomId} via SignalR",
            userId, username, roomId);

        // Уведомляем всех в комнате
        await Clients.Group(groupName).SendAsync("PlayerLeft", new
        {
            userId,
            username,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Начать новый раунд (только владелец)
    /// </summary>
    public async Task StartRound(Guid sessionId)
    {
        var userId = GetUserId();

        var session = await _unitOfWork.GameSessions.GetWithDetailsAsync(sessionId);
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session not found");
            return;
        }

        var room = session.Room;

        // Проверка прав владельца
        if (room.OwnerUserId != userId)
        {
            await Clients.Caller.SendAsync("Error", "Only room owner can start rounds");
            return;
        }

        if (session.CurrentRoundId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "A round is already in progress");
            return;
        }

        try
        {
            // Получаем случайный вопрос
            var tagIds = room.TagSelections.Select(ts => ts.TagId).ToArray();
            var question = await _unitOfWork.Questions.GetRandomApprovedAsync(tagIds.Any() ? tagIds : null);

            if (question == null)
            {
                await Clients.Caller.SendAsync("Error", "No approved questions available");
                return;
            }

            // Создаем раунд
            var round = session.AddRound(question.Id, 60);
            session.StartRound(round.Id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Round {RoundId} started in session {SessionId}", round.Id, sessionId);

            // Уведомляем всех игроков
            var groupName = GetRoomGroupName(room.Id);
            await Clients.Group(groupName).SendAsync("RoundStarted", new
            {
                roundId = round.Id,
                roundIndex = round.RoundIndex,
                questionId = question.Id,
                promptText = question.PromptText,
                title = question.Title,
                difficulty = question.Difficulty.ToString(),
                timeLimitSec = round.TimeLimitSec,
                startedAt = round.StartedAt,
                hints = question.Hints.OrderBy(h => h.OrderIndex).Select(h => new
                {
                    orderIndex = h.OrderIndex,
                    hintText = h.HintText,
                    revealTimeSec = h.RevealTimeSec
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting round for session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Отправить ответ на вопрос
    /// </summary>
    public async Task SubmitAnswer(Guid roundId, string answerText)
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Unknown";

        _logger.LogInformation("📥 [SubmitAnswer] ========== ANSWER SUBMISSION START ==========");
        _logger.LogInformation("📥 [SubmitAnswer] User: {Username} ({UserId})", username, userId);
        _logger.LogInformation("📥 [SubmitAnswer] Round ID: {RoundId}", roundId);
        _logger.LogInformation("📥 [SubmitAnswer] Answer Text: '{AnswerText}'", answerText);

        _logger.LogInformation("🔍 [SubmitAnswer] Loading round with details...");
        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
        if (round == null)
        {
            _logger.LogError("❌ [SubmitAnswer] Round {RoundId} not found!", roundId);
            await Clients.Caller.SendAsync("Error", "Round not found");
            return;
        }
        _logger.LogInformation("✅ [SubmitAnswer] Round loaded. Question ID: {QuestionId}, Status: {Status}", round.QuestionId, round.Status);

        if (round.Status != RoundStatus.Active)
        {
            _logger.LogWarning("⚠️ [SubmitAnswer] Round is not active! Status: {Status}", round.Status);
            await Clients.Caller.SendAsync("Error", "Round is not active");
            return;
        }

        if (round.IsDeadlinePassed())
        {
            _logger.LogWarning("⚠️ [SubmitAnswer] Time limit expired!");
            await Clients.Caller.SendAsync("Error", "Time limit expired");
            return;
        }

        // Проверяем, не ответил ли игрок уже ПРАВИЛЬНО
        _logger.LogInformation("🔍 [SubmitAnswer] Checking if user already answered correctly...");
        var previousCorrectAnswer = round.Answers
            .Where(a => a.UserId == userId && a.Evaluation != null && a.Evaluation.IsCorrect)
            .FirstOrDefault();
            
        if (previousCorrectAnswer != null)
        {
            _logger.LogWarning("⚠️ [SubmitAnswer] User already answered correctly! Blocking duplicate.");
            await Clients.Caller.SendAsync("Error", "You have already answered correctly. Wait for round to end.");
            return;
        }
        _logger.LogInformation("✅ [SubmitAnswer] User can submit answer (no correct answer yet)");

        try
        {
            var answerTimeMs = round.GetElapsedTimeMs();
            _logger.LogInformation("⏱️ [SubmitAnswer] Answer time: {TimeMs}ms ({TimeSec}s)", answerTimeMs, answerTimeMs / 1000.0);
            
            _logger.LogInformation("💾 [SubmitAnswer] Adding answer to round...");
            var playerAnswer = round.AddAnswer(userId, answerText, answerTimeMs);
            _logger.LogInformation("✅ [SubmitAnswer] PlayerAnswer created: {AnswerId}", playerAnswer.Id);
            
            _logger.LogInformation("💾 [SubmitAnswer] Saving PlayerAnswer to database...");
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("✅ [SubmitAnswer] PlayerAnswer saved");

            // Оцениваем ответ
            _logger.LogInformation("🎯 [SubmitAnswer] Calling FuzzyMatchingService.EvaluateAnswerAsync...");
            var matchResult = await _fuzzyMatchingService.EvaluateAnswerAsync(
                round.QuestionId,
                answerText);
            
            _logger.LogInformation("✅ [SubmitAnswer] FuzzyMatching complete!");
            _logger.LogInformation("📊 [SubmitAnswer] Result: IsCorrect={IsCorrect}, Strategy={Strategy}, Confidence={Confidence}", 
                matchResult.IsCorrect, matchResult.Strategy, matchResult.Confidence);

            _logger.LogInformation("📝 [SubmitAnswer] Creating AnswerEvaluation...");
            var confidence = Domain.ValueObjects.Confidence.Create(matchResult.Confidence);
            var scoreAwarded = matchResult.IsCorrect ? CalculateScore(answerTimeMs, round.TimeLimitSec) : 0;
            
            _logger.LogInformation("💯 [SubmitAnswer] Score calculated: {Score} points (IsCorrect: {IsCorrect})", scoreAwarded, matchResult.IsCorrect);

            var evaluation = new Domain.Entities.AnswerEvaluation(
                playerAnswer.Id,
                matchResult.IsCorrect,
                matchResult.Strategy,
                scoreAwarded,
                confidence,
                matchResult.NormalizedAnswer,
                matchResult.MatchedQuestionAnswerId,
                matchResult.MatchedAliasId);
            
            _logger.LogInformation("✅ [SubmitAnswer] AnswerEvaluation created");

            _logger.LogInformation("📝 [SubmitAnswer] Setting evaluation on PlayerAnswer...");
            playerAnswer.SetEvaluation(evaluation);
            _logger.LogInformation("✅ [SubmitAnswer] Evaluation set");

            // Обновляем scoreboard
            _logger.LogInformation("📊 [SubmitAnswer] Loading scoreboard...");
            var scoreboard = await _unitOfWork.Scoreboards.GetBySessionAndUserAsync(round.SessionId, userId);
            var isFirstCorrect = false;

            if (scoreboard != null)
            {
                _logger.LogInformation("📊 [SubmitAnswer] Scoreboard found. Current score: {CurrentScore}", scoreboard.ScoreTotal);
                
                isFirstCorrect = matchResult.IsCorrect && !round.Answers.Any(a =>
                    a.UserId != userId &&
                    a.Evaluation != null &&
                    a.Evaluation.IsCorrect);
                
                _logger.LogInformation("🏆 [SubmitAnswer] Is first correct answer: {IsFirst}", isFirstCorrect);

                _logger.LogInformation("📊 [SubmitAnswer] Adding score to scoreboard...");
                scoreboard.AddScore(scoreAwarded, matchResult.IsCorrect, isFirstCorrect);
                _logger.LogInformation("📊 [SubmitAnswer] New total score: {NewTotal}", scoreboard.ScoreTotal);

                if (isFirstCorrect)
                {
                    _logger.LogInformation("👑 [SubmitAnswer] Setting user as round winner");
                    round.SetWinner(userId);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ [SubmitAnswer] Scoreboard not found for user {UserId} in session {SessionId}!", userId, round.SessionId);
            }

            _logger.LogInformation("💾 [SubmitAnswer] Saving evaluation and scoreboard...");
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("✅ [SubmitAnswer] Saved successfully");

            _logger.LogInformation(
                "✅ [SubmitAnswer] Answer submitted in round {RoundId} by {Username}: IsCorrect={IsCorrect}, Score={Score}",
                roundId, username, matchResult.IsCorrect, scoreAwarded);

            _logger.LogInformation("🔍 [SubmitAnswer] Loading session and room...");
            var session = await _unitOfWork.GameSessions.GetByIdAsync(round.SessionId);
            
            if (session == null)
            {
                _logger.LogError("❌ [SubmitAnswer] Session {SessionId} not found!", round.SessionId);
                await Clients.Caller.SendAsync("Error", "Session not found");
                return;
            }
            
            _logger.LogInformation("✅ [SubmitAnswer] Session loaded: {SessionId}", session.Id);
            
            // Загружаем Room отдельно, так как он не загружается автоматически
            var room = await _unitOfWork.Rooms.GetByIdAsync(session.RoomId);
            
            if (room == null)
            {
                _logger.LogError("❌ [SubmitAnswer] Room {RoomId} not found!", session.RoomId);
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }
            var groupName = GetRoomGroupName(room.Id);
            _logger.LogInformation("✅ [SubmitAnswer] Session and room loaded. Group: {GroupName}", groupName);

            // Отправляем результат игроку
            _logger.LogInformation("📡 [SubmitAnswer] Sending AnswerResult to caller...");
            await Clients.Caller.SendAsync("AnswerResult", new
            {
                isCorrect = matchResult.IsCorrect,
                strategy = matchResult.Strategy.ToString(),
                scoreAwarded,
                confidence = matchResult.Confidence,
                isFirstCorrect,
                newTotalScore = scoreboard?.ScoreTotal ?? 0,
                answerTimeMs
            });
            _logger.LogInformation("✅ [SubmitAnswer] AnswerResult sent to caller");

            // Уведомляем всех о том, что кто-то ответил (без деталей)
            _logger.LogInformation("📡 [SubmitAnswer] Broadcasting PlayerAnswered to group...");
            await Clients.Group(groupName).SendAsync("PlayerAnswered", new
            {
                userId,
                username,
                timestamp = DateTime.UtcNow,
                answeredCount = round.Answers.Count,
                isCorrect = matchResult.IsCorrect
            });
            _logger.LogInformation("✅ [SubmitAnswer] PlayerAnswered broadcast complete");

            // Если это первый правильный ответ, уведомляем всех
            if (isFirstCorrect)
            {
                _logger.LogInformation("📡 [SubmitAnswer] Broadcasting FirstCorrectAnswer (user is first!)...");
                await Clients.Group(groupName).SendAsync("FirstCorrectAnswer", new
                {
                    userId,
                    username,
                    scoreAwarded,
                    timestamp = DateTime.UtcNow
                });
                _logger.LogInformation("✅ [SubmitAnswer] FirstCorrectAnswer broadcast complete");
            }
            
            // 🎯 КРИТИЧНО: Проверяем условия автоматического завершения раунда
            _logger.LogInformation("🔍 [SubmitAnswer] Checking auto-end conditions...");
            
            // ВАЖНО: Перезагружаем раунд с актуальными ответами!
            var updatedRound = await _unitOfWork.GameRounds.GetWithDetailsAsync(round.Id);
            if (updatedRound != null)
            {
                await CheckAndAutoEndRound(updatedRound, session, room, groupName);
            }
            
            _logger.LogInformation("📥 [SubmitAnswer] ========== ANSWER SUBMISSION COMPLETE ==========");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SubmitAnswer] CRITICAL ERROR submitting answer for round {RoundId}", roundId);
            _logger.LogError(ex, "   User: {Username} ({UserId})", username, userId);
            _logger.LogError(ex, "   Answer: '{AnswerText}'", answerText);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Завершить раунд (только владелец)
    /// </summary>
    public async Task EndRound(Guid roundId)
    {
        var userId = GetUserId();

        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
        if (round == null)
        {
            await Clients.Caller.SendAsync("Error", "Round not found");
            return;
        }

        var session = await _unitOfWork.GameSessions.GetByIdAsync(round.SessionId);
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session not found");
            return;
        }

        var room = session.Room;

        // Проверка прав
        if (room.OwnerUserId != userId)
        {
            await Clients.Caller.SendAsync("Error", "Only room owner can end rounds");
            return;
        }

        try
        {
            session.EndRound(roundId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Round {RoundId} ended by owner", roundId);

            // Получаем результаты
            var results = round.Answers
                .Where(a => a.Evaluation != null)
                .OrderByDescending(a => a.Evaluation!.ScoreAwarded)
                .Select(a => new
                {
                    userId = a.UserId,
                    username = a.User.Username,
                    answerText = a.AnswerText,
                    isCorrect = a.Evaluation!.IsCorrect,
                    scoreAwarded = a.Evaluation.ScoreAwarded,
                    answerTimeMs = a.AnswerTimeMs,
                    strategy = a.Evaluation.MatchStrategy.ToString()
                })
                .ToList();

            // Получаем обновленный scoreboard
            var scoreboards = await _unitOfWork.Scoreboards.GetLeaderboardAsync(session.Id, 10);

            var groupName = GetRoomGroupName(room.Id);

            // Уведомляем всех о завершении раунда
            await Clients.Group(groupName).SendAsync("RoundEnded", new
            {
                roundId,
                winnerId = round.WinnerUserId,
                correctAnswer = round.Question.Answers.FirstOrDefault(a => a.IsPrimary)?.AnswerText,
                results,
                scoreboard = scoreboards.Select((s, index) => new
                {
                    rank = index + 1,
                    userId = s.UserId,
                    username = s.User.Username,
                    scoreTotal = s.ScoreTotal,
                    correctCount = s.CorrectCount
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending round {RoundId}", roundId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var connectionId = Context.ConnectionId;
        
        _logger.LogInformation("🔌 [GameHub] CONNECTED - ConnectionId: {ConnectionId}, UserId: {UserId}, Username: {Username}", 
            connectionId, userId, username);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;
        
        _logger.LogInformation("🔌 [GameHub] DISCONNECTED - ConnectionId: {ConnectionId}, UserId: {UserId}", 
            connectionId, userId);
        
        if (exception != null)
        {
            _logger.LogError(exception, "❌ [GameHub] Disconnection error for ConnectionId: {ConnectionId}", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    private static string GetRoomGroupName(Guid roomId) => $"{RoomGroupPrefix}{roomId}";

    private int CalculateScore(int answerTimeMs, int timeLimitSec)
    {
        const int baseScore = 100;
        const int timeBonus = 50;

        int score = baseScore;
        var timeLimitMs = timeLimitSec * 1000;
        var speedRatio = 1.0 - ((double)answerTimeMs / timeLimitMs);
        score += (int)(timeBonus * Math.Max(0, speedRatio));

        return score;
    }
    
    /// <summary>
    /// Проверяет условия завершения раунда и автоматически завершает если нужно
    /// </summary>
    private async Task CheckAndAutoEndRound(GameRound round, GameSession session, Room room, string groupName)
    {
        try
        {
            // Получаем количество активных игроков из БД (session.Players может быть пустым!)
            _logger.LogInformation("🔍 [AutoEnd] Checking round {RoundId}...", round.Id);
            
            // Загружаем игроков из Scoreboard (они там точно есть если играют)
            var scoreboards = await _unitOfWork.Scoreboards.GetBySessionIdAsync(session.Id);
            var totalPlayers = scoreboards.Count;
            
            _logger.LogInformation("📊 [AutoEnd] Total players in session: {Total}", totalPlayers);
            
            // ВАЖНО: Если игроков нет - НЕ завершаем раунд!
            if (totalPlayers == 0)
            {
                _logger.LogWarning("⚠️ [AutoEnd] No players found in session! Skipping auto-end check.");
                return;
            }
            
            // Подсчитываем игроков с правильными ответами
            var playersWithCorrectAnswers = round.Answers
                .Where(a => a.Evaluation != null && a.Evaluation.IsCorrect)
                .Select(a => a.UserId)
                .Distinct()
                .Count();
                
            _logger.LogInformation("📊 [AutoEnd] Players answered correctly: {Correct}/{Total}", 
                playersWithCorrectAnswers, totalPlayers);
            
            // Условие 1: Все игроки ответили правильно (и их больше 0!)
            if (playersWithCorrectAnswers >= totalPlayers && totalPlayers > 0)
            {
                _logger.LogInformation("✅ [AutoEnd] All players answered correctly! Ending round...");
                await AutoEndRoundAndStartNext(round.Id, session, room, groupName, "All players answered");
                return;
            }
            
            // Условие 2: Время истекло (проверяется таймером в клиенте, но можем добавить и тут)
            if (round.IsDeadlinePassed())
            {
                _logger.LogInformation("⏰ [AutoEnd] Time expired! Ending round...");
                await AutoEndRoundAndStartNext(round.Id, session, room, groupName, "Time expired");
                return;
            }
            
            _logger.LogInformation("⏳ [AutoEnd] Round continues: {Correct}/{Total} answered", 
                playersWithCorrectAnswers, totalPlayers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AutoEnd] Error checking auto-end conditions");
        }
    }
    
    /// <summary>
    /// Автоматически завершает раунд и запускает следующий
    /// </summary>
    private async Task AutoEndRoundAndStartNext(Guid roundId, GameSession session, Room room, string groupName, string reason)
    {
        try
        {
            _logger.LogInformation("🔄 [AutoEnd] Ending round {RoundId}. Reason: {Reason}", roundId, reason);
            
            var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
            if (round == null || round.Status != RoundStatus.Active)
            {
                _logger.LogWarning("⚠️ [AutoEnd] Round {RoundId} not found or not active", roundId);
                return;
            }
            
            // Завершаем раунд
            session.EndRound(roundId);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("✅ [AutoEnd] Round {RoundId} ended", roundId);
            
            // Получаем результаты раунда
            var results = round.Answers
                .Where(a => a.Evaluation != null)
                .OrderByDescending(a => a.Evaluation!.ScoreAwarded)
                .Select(a => new
                {
                    userId = a.UserId,
                    username = a.User.Username,
                    answerText = a.AnswerText,
                    isCorrect = a.Evaluation!.IsCorrect,
                    scoreAwarded = a.Evaluation.ScoreAwarded,
                    answerTimeMs = a.AnswerTimeMs,
                    strategy = a.Evaluation.MatchStrategy.ToString()
                })
                .ToList();
            
            // Получаем правильный ответ
            var correctAnswer = round.Question.Answers.FirstOrDefault(a => a.IsPrimary)?.AnswerText ?? "Unknown";
            
            // Получаем обновленный scoreboard
            var scoreboards = await _unitOfWork.Scoreboards.GetLeaderboardAsync(session.Id, 10);
            
            // Уведомляем всех о завершении раунда
            await Clients.Group(groupName).SendAsync("RoundEnded", new
            {
                roundId,
                winnerId = round.WinnerUserId,
                correctAnswer,
                reason,
                results,
                scoreboard = scoreboards.Select((s, index) => new
                {
                    rank = index + 1,
                    userId = s.UserId,
                    username = s.User.Username,
                    scoreTotal = s.ScoreTotal,
                    correctCount = s.CorrectCount
                })
            });
            
            _logger.LogInformation("📡 [AutoEnd] RoundEnded notification sent to group");
            
            // Пауза 5 секунд перед следующим раундом
            await Task.Delay(5000);
            
            // Проверяем, не закончилась ли игра
            if (session.TotalRoundsPlayed >= session.TotalRoundsPlanned)
            {
                _logger.LogInformation("🏁 [AutoEnd] Game finished! Total rounds: {Total}", session.TotalRoundsPlayed);
                
                session.Finish();
                room.FinishGame();
                await _unitOfWork.SaveChangesAsync();
                
                await Clients.Group(groupName).SendAsync("GameFinished", new
                {
                    sessionId = session.Id,
                    finalScoreboard = scoreboards.Select((s, index) => new
                    {
                        rank = index + 1,
                        userId = s.UserId,
                        username = s.User.Username,
                        scoreTotal = s.ScoreTotal,
                        correctCount = s.CorrectCount
                    })
                });
                
                _logger.LogInformation("🎉 [AutoEnd] Game finished notification sent!");
                return;
            }
            
            // Создаем следующий раунд
            _logger.LogInformation("🎲 [AutoEnd] Creating next round...");
            
            var tagIds = room.TagSelections.Select(ts => ts.TagId).ToArray();
            var question = await _unitOfWork.Questions.GetRandomApprovedAsync(tagIds.Any() ? tagIds : null);
            
            if (question == null)
            {
                _logger.LogError("❌ [AutoEnd] No more questions available!");
                await Clients.Group(groupName).SendAsync("Error", "No more questions available");
                return;
            }
            
            var nextRound = session.AddRound(question.Id, round.TimeLimitSec);
            session.StartRound(nextRound.Id);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("✅ [AutoEnd] Next round created: {RoundId}", nextRound.Id);
            
            // Уведомляем всех о новом раунде
            var roundStartedData = new
            {
                roundId = nextRound.Id,
                roundIndex = nextRound.RoundIndex,
                questionId = question.Id,
                text = question.PromptText,  // Клиент ожидает "text", а не "promptText"
                title = question.Title,
                difficulty = question.Difficulty.ToString(),
                timeLimit = nextRound.TimeLimitSec,  // Клиент ожидает "timeLimit"
                startedAt = nextRound.StartedAt,
                questionType = question.Type.ToString(),
                mediaUrl = (string?)null,  // TODO: загрузка медиа
                hints = question.Hints.OrderBy(h => h.OrderIndex).Select(h => new
                {
                    orderIndex = h.OrderIndex,
                    text = h.HintText,  // Клиент ожидает "text"
                    revealTimeSeconds = h.RevealTimeSec  // Клиент ожидает "revealTimeSeconds"
                }).ToList()
            };
            
            var roundStartedJson = System.Text.Json.JsonSerializer.Serialize(roundStartedData);
            
            _logger.LogInformation("📡 [AutoEnd] Sending RoundStarted to group {GroupName}...", groupName);
            _logger.LogInformation("   Data: {Data}", roundStartedJson);
            _logger.LogInformation("   Group members count: checking...");
            
            // Отправляем в группу
            await Clients.Group(groupName).SendAsync("RoundStarted", roundStartedJson);
            _logger.LogInformation("✅ [AutoEnd] RoundStarted sent to group");
            
            // ДОПОЛНИТЕЛЬНО: отправляем всем клиентам в Hub (на случай если кто-то не в группе)
            _logger.LogInformation("📡 [AutoEnd] Also broadcasting to ALL clients in session {SessionId}...", session.Id);
            await Clients.All.SendAsync("RoundStarted", roundStartedJson);
            _logger.LogInformation("✅ [AutoEnd] RoundStarted broadcast to all");
            
            _logger.LogInformation("🎉 [AutoEnd] Next round started successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AutoEnd] Error in AutoEndRoundAndStartNext");
        }
    }
}
