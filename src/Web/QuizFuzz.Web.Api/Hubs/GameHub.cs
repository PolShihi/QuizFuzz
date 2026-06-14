using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Web.Api.Services;

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
    private readonly IHintRevealScheduler _hintRevealScheduler;

    // Group names format: "room:{roomId}"
    private const string RoomGroupPrefix = "room:";
    
    public GameHub(
        IUnitOfWork unitOfWork,
        IFuzzyMatchingService fuzzyMatchingService,
        ILogger<GameHub> logger,
        IHintRevealScheduler hintRevealScheduler)
    {
        _unitOfWork = unitOfWork;
        _fuzzyMatchingService = fuzzyMatchingService;
        _logger = logger;
        _hintRevealScheduler = hintRevealScheduler;
    }

    /// <summary>
    /// Присоединиться к игровой сессии (используется при старте игры)
    /// </summary>
    public async Task JoinGame(string sessionId)
    {
        _logger.LogDebug(" [GameHub] ========== JOIN GAME REQUEST ==========");
        _logger.LogDebug(" [GameHub] SessionId: {SessionId}", sessionId);
        _logger.LogDebug(" [GameHub] ConnectionId: {ConnectionId}", Context.ConnectionId);
        
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            _logger.LogDebug(" [GameHub] Invalid SessionId format: {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "Invalid session ID format");
            return;
        }

        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Unknown";
        
        _logger.LogDebug(" [GameHub] UserId: {UserId}, Username: {Username}", userId, username);

        var session = await _unitOfWork.GameSessions.GetWithDetailsAsync(parsedSessionId);
        if (session == null)
        {
            _logger.LogDebug(" [GameHub] Session not found: {SessionId}", parsedSessionId);
            await Clients.Caller.SendAsync("Error", "Session not found");
            return;
        }

        _logger.LogDebug(" [GameHub] Session found: {SessionId}, Status: {Status}", 
            parsedSessionId, session.Status);

        var room = session.Room;
        _logger.LogDebug(" [GameHub] Room: {RoomId}, Name: {RoomName}", room.Id, room.Name);

        // Проверяем, что игрок в сессии
        var player = session.Players.FirstOrDefault(p => p.UserId == userId && p.IsActive);
        if (player == null)
        {
            _logger.LogDebug(" [GameHub] User {UserId} is not a player in session {SessionId}", 
                userId, parsedSessionId);
            await Clients.Caller.SendAsync("Error", "You are not a player in this session");
            return;
        }

        _logger.LogDebug(" [GameHub] Player found in session");

        var groupName = GetRoomGroupName(room.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug(" [GameHub] Added to group: {GroupName}", groupName);

        // Уведомляем всех в комнате
        await Clients.Group(groupName).SendAsync("PlayerJoined", new
        {
            userId,
            username,
            timestamp = DateTime.UtcNow
        });
        
        _logger.LogDebug(" [GameHub] Sent PlayerJoined to group");

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

        if (session.CurrentRoundId.HasValue)
        {
            _hintRevealScheduler.EnsureHintsScheduled(room.Id, session.CurrentRoundId.Value);
            _logger.LogDebug(" [GameHub] Ensured hint scheduler for active round {RoundId}", session.CurrentRoundId.Value);
        }
        
        _logger.LogDebug(" [GameHub] Sent GameState to caller");
        _logger.LogDebug(" [GameHub] ========== JOIN GAME SUCCESS ==========");
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

        _logger.LogDebug(
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

        _logger.LogDebug(
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
            //  КРИТИЧНО: Загружаем комнату с TagSelections!
            var roomWithTags = await _unitOfWork.Rooms.GetWithDetailsAsync(room.Id);
            if (roomWithTags == null)
            {
                _logger.LogDebug(" [StartRound] Room {RoomId} not found!", room.Id);
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }
            
            // Получаем случайный вопрос с учетом фильтров
            var tagIds = roomWithTags.TagSelections.Select(ts => ts.TagId).ToArray();
            var difficultyFilters = roomWithTags.GetDifficultyFilters();
            
            _logger.LogDebug(" [StartRound] ========== SELECTING QUESTION ==========");
            _logger.LogDebug(" [StartRound] Room: {RoomId} - {RoomName}", roomWithTags.Id, roomWithTags.Name);
            _logger.LogDebug(" [StartRound] TagSelections count: {Count}", roomWithTags.TagSelections.Count);
            
            if (roomWithTags.TagSelections.Any())
            {
                _logger.LogDebug(" [StartRound] Tag filters ({Count}):", tagIds.Length);
                foreach (var ts in roomWithTags.TagSelections)
                {
                    _logger.LogDebug("    TagId: {TagId}, Weight: {Weight}", ts.TagId, ts.Weight);
                }
            }
            else
            {
                _logger.LogDebug(" [StartRound] NO TAG FILTERS (all categories)");
            }
            
            if (difficultyFilters.Any())
            {
                _logger.LogDebug(" [StartRound] Difficulty filters: {Filters}", string.Join(", ", difficultyFilters));
            }
            else
            {
                _logger.LogDebug(" [StartRound] NO DIFFICULTY FILTERS (all levels)");
            }
            
            
            var question = await _unitOfWork.Questions.GetRandomApprovedWithFiltersAsync(
                tagIds.Any() ? tagIds : null,
                difficultyFilters.Any() ? difficultyFilters : null);

            if (question == null)
            {
                _logger.LogDebug(" [StartRound] No approved questions available with specified filters!");
                _logger.LogDebug("   Tags filter: {Tags}", tagIds.Any() ? string.Join(", ", tagIds) : "None");
                _logger.LogDebug("   Difficulty filter: {Diff}", difficultyFilters.Any() ? string.Join(", ", difficultyFilters) : "None");
                await Clients.Caller.SendAsync("Error", "No approved questions available with specified filters");
                return;
            }
            
            _logger.LogDebug(" [StartRound] Question selected: {QuestionId}, Difficulty: {Difficulty}", 
                question.Id, question.Difficulty);

            // Создаем раунд
            var round = session.AddRound(question.Id, roomWithTags.RoundTimeLimitSec);
            session.StartRound(round.Id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Round {RoundId} started in session {SessionId}", round.Id, sessionId);

            // Получаем MediaUrl если есть и конвертируем в ПОЛНЫЙ URL
            var mediaAsset = question.MediaAssets.FirstOrDefault();
            var mediaUrl = ConvertToFullMediaUrl(mediaAsset?.Url);
            
            _logger.LogDebug(" [StartRound] Question type: {Type}, Media: {HasMedia}", 
                question.Type, mediaUrl != null ? "YES" : "NO");
            
            if (mediaUrl != null)
            {
                _logger.LogDebug("    Media FULL URL: {Url}", mediaUrl);
            }

            // Уведомляем всех игроков.
            // Важно: клиент Game.razor ожидает JSON string и имена полей из GameQuestionDto
            // (text/timeLimit/hints[].text/revealTimeSeconds), поэтому не отправляем anonymous object напрямую.
            var groupName = GetRoomGroupName(room.Id);
            var roundStartedData = new
            {
                roundId = round.Id,
                roundIndex = round.RoundIndex,
                questionId = question.Id,
                text = question.PromptText,
                title = question.Title,
                difficulty = question.Difficulty.ToString(),
                questionType = question.Type.ToString(),
                mediaUrl = mediaUrl,
                timeLimit = round.TimeLimitSec,
                startedAt = round.StartedAt,
                hints = Array.Empty<object>()
            };

            var roundStartedJson = System.Text.Json.JsonSerializer.Serialize(roundStartedData);
            await Clients.Group(groupName).SendAsync("RoundStarted", roundStartedJson);
            _hintRevealScheduler.ScheduleHints(room.Id, round.Id);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error starting round for session {SessionId}", sessionId);
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

        _logger.LogDebug(" [SubmitAnswer] ========== ANSWER SUBMISSION START ==========");
        _logger.LogDebug(" [SubmitAnswer] User: {Username} ({UserId})", username, userId);
        _logger.LogDebug(" [SubmitAnswer] Round ID: {RoundId}", roundId);
        _logger.LogDebug(" [SubmitAnswer] Answer Text: '{AnswerText}'", answerText);

        _logger.LogDebug(" [SubmitAnswer] Loading round with details...");
        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
        if (round == null)
        {
            _logger.LogDebug(" [SubmitAnswer] Round {RoundId} not found!", roundId);
            await Clients.Caller.SendAsync("Error", "Round not found");
            return;
        }
        _logger.LogDebug(" [SubmitAnswer] Round loaded. Question ID: {QuestionId}, Status: {Status}", round.QuestionId, round.Status);

        if (round.Status != RoundStatus.Active)
        {
            _logger.LogDebug(" [SubmitAnswer] Round is not active! Status: {Status}", round.Status);
            await Clients.Caller.SendAsync("Error", "Round is not active");
            return;
        }

        if (round.IsDeadlinePassed())
        {
            _logger.LogDebug(" [SubmitAnswer] Time limit expired!");
            await Clients.Caller.SendAsync("Error", "Time limit expired");
            return;
        }

        // Проверяем, не ответил ли игрок уже ПРАВИЛЬНО
        _logger.LogDebug(" [SubmitAnswer] Checking if user already answered correctly...");
        var previousCorrectAnswer = round.Answers
            .Where(a => a.UserId == userId && a.Evaluation != null && a.Evaluation.IsCorrect)
            .FirstOrDefault();
            
        if (previousCorrectAnswer != null)
        {
            _logger.LogDebug(" [SubmitAnswer] User already answered correctly! Blocking duplicate.");
            await Clients.Caller.SendAsync("Error", "You have already answered correctly. Wait for round to end.");
            return;
        }
        _logger.LogDebug(" [SubmitAnswer] User can submit answer (no correct answer yet)");

        try
        {
            var answerTimeMs = round.GetElapsedTimeMs();
            _logger.LogDebug("⏱ [SubmitAnswer] Answer time: {TimeMs}ms ({TimeSec}s)", answerTimeMs, answerTimeMs / 1000.0);
            
            _logger.LogDebug(" [SubmitAnswer] Adding answer to round...");
            var playerAnswer = round.AddAnswer(userId, answerText, answerTimeMs);
            _logger.LogDebug(" [SubmitAnswer] PlayerAnswer created: {AnswerId}", playerAnswer.Id);
            
            _logger.LogDebug(" [SubmitAnswer] Saving PlayerAnswer to database...");
            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug(" [SubmitAnswer] PlayerAnswer saved");

            // Оцениваем ответ
            _logger.LogDebug(" [SubmitAnswer] Calling FuzzyMatchingService.EvaluateAnswerAsync...");
            var matchResult = await _fuzzyMatchingService.EvaluateAnswerAsync(
                round.QuestionId,
                answerText);
            
            _logger.LogDebug(" [SubmitAnswer] FuzzyMatching complete!");
            _logger.LogDebug(" [SubmitAnswer] Result: IsCorrect={IsCorrect}, Strategy={Strategy}, Confidence={Confidence}", 
                matchResult.IsCorrect, matchResult.Strategy, matchResult.Confidence);

            _logger.LogDebug(" [SubmitAnswer] Creating AnswerEvaluation...");
            var confidence = Domain.ValueObjects.Confidence.Create(matchResult.Confidence);
            var scoreAwarded = matchResult.IsCorrect ? CalculateScore(answerTimeMs, round.TimeLimitSec, GetRevealedHintsCount(round, answerTimeMs)) : 0;
            
            _logger.LogDebug(" [SubmitAnswer] Score calculated: {Score} points (IsCorrect: {IsCorrect})", scoreAwarded, matchResult.IsCorrect);

            var evaluation = new Domain.Entities.AnswerEvaluation(
                playerAnswer.Id,
                matchResult.IsCorrect,
                matchResult.Strategy,
                scoreAwarded,
                confidence,
                matchResult.NormalizedAnswer,
                matchResult.MatchedQuestionAnswerId,
                matchResult.MatchedAliasId);
            
            _logger.LogDebug(" [SubmitAnswer] AnswerEvaluation created");

            _logger.LogDebug(" [SubmitAnswer] Setting evaluation on PlayerAnswer...");
            playerAnswer.SetEvaluation(evaluation);
            _logger.LogDebug(" [SubmitAnswer] Evaluation set");

            // Обновляем scoreboard
            _logger.LogDebug(" [SubmitAnswer] Loading scoreboard...");
            var scoreboard = await _unitOfWork.Scoreboards.GetBySessionAndUserAsync(round.SessionId, userId);
            var isFirstCorrect = false;

            if (scoreboard != null)
            {
                _logger.LogDebug(" [SubmitAnswer] Scoreboard found. Current score: {CurrentScore}", scoreboard.ScoreTotal);
                
                isFirstCorrect = matchResult.IsCorrect && !round.Answers.Any(a =>
                    a.UserId != userId &&
                    a.Evaluation != null &&
                    a.Evaluation.IsCorrect);
                
                _logger.LogDebug(" [SubmitAnswer] Is first correct answer: {IsFirst}", isFirstCorrect);

                _logger.LogDebug(" [SubmitAnswer] Adding score to scoreboard...");
                scoreboard.AddScore(scoreAwarded, matchResult.IsCorrect, isFirstCorrect);
                _logger.LogDebug(" [SubmitAnswer] New total score: {NewTotal}", scoreboard.ScoreTotal);

                if (isFirstCorrect)
                {
                    _logger.LogDebug(" [SubmitAnswer] Setting user as round winner");
                    round.SetWinner(userId);
                }
            }
            else
            {
                _logger.LogDebug(" [SubmitAnswer] Scoreboard not found for user {UserId} in session {SessionId}!", userId, round.SessionId);
            }

            _logger.LogDebug(" [SubmitAnswer] Saving evaluation and scoreboard...");
            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug(" [SubmitAnswer] Saved successfully");

            _logger.LogDebug(
                " [SubmitAnswer] Answer submitted in round {RoundId} by {Username}: IsCorrect={IsCorrect}, Score={Score}",
                roundId, username, matchResult.IsCorrect, scoreAwarded);

            _logger.LogDebug(" [SubmitAnswer] Loading session and room...");
            var session = await _unitOfWork.GameSessions.GetByIdAsync(round.SessionId);
            
            if (session == null)
            {
                _logger.LogDebug(" [SubmitAnswer] Session {SessionId} not found!", round.SessionId);
                await Clients.Caller.SendAsync("Error", "Session not found");
                return;
            }
            
            _logger.LogDebug(" [SubmitAnswer] Session loaded: {SessionId}", session.Id);
            
            // Загружаем Room отдельно, так как он не загружается автоматически
            var room = await _unitOfWork.Rooms.GetByIdAsync(session.RoomId);
            
            if (room == null)
            {
                _logger.LogDebug(" [SubmitAnswer] Room {RoomId} not found!", session.RoomId);
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }
            var groupName = GetRoomGroupName(room.Id);
            _logger.LogDebug(" [SubmitAnswer] Session and room loaded. Group: {GroupName}", groupName);

            // Отправляем результат игроку
            _logger.LogDebug(" [SubmitAnswer] Sending AnswerResult to caller...");
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
            _logger.LogDebug(" [SubmitAnswer] AnswerResult sent to caller");

            // Уведомляем всех о том, что кто-то ответил (без деталей)
            _logger.LogDebug(" [SubmitAnswer] Broadcasting PlayerAnswered to group...");
            await Clients.Group(groupName).SendAsync("PlayerAnswered", new
            {
                userId,
                username,
                timestamp = DateTime.UtcNow,
                answeredCount = round.Answers.Count,
                isCorrect = matchResult.IsCorrect
            });
            _logger.LogDebug(" [SubmitAnswer] PlayerAnswered broadcast complete");

            // Если это первый правильный ответ, уведомляем всех
            if (isFirstCorrect)
            {
                _logger.LogDebug(" [SubmitAnswer] Broadcasting FirstCorrectAnswer (user is first!)...");
                await Clients.Group(groupName).SendAsync("FirstCorrectAnswer", new
                {
                    userId,
                    username,
                    scoreAwarded,
                    timestamp = DateTime.UtcNow
                });
                _logger.LogDebug(" [SubmitAnswer] FirstCorrectAnswer broadcast complete");
            }
            
            //  КРИТИЧНО: Проверяем условия автоматического завершения раунда
            _logger.LogDebug(" [SubmitAnswer] Checking auto-end conditions...");
            
            // ВАЖНО: Перезагружаем раунд с актуальными ответами!
            var updatedRound = await _unitOfWork.GameRounds.GetWithDetailsAsync(round.Id);
            if (updatedRound != null)
            {
                await CheckAndAutoEndRound(updatedRound, session, room, groupName);
            }
            
            _logger.LogDebug(" [SubmitAnswer] ========== ANSWER SUBMISSION COMPLETE ==========");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [SubmitAnswer] CRITICAL ERROR submitting answer for round {RoundId}", roundId);
            _logger.LogDebug(ex, "   User: {Username} ({UserId})", username, userId);
            _logger.LogDebug(ex, "   Answer: '{AnswerText}'", answerText);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Клиент сообщает, что его таймер дошёл до нуля.
    /// Сервер сам проверяет deadline и только после этого завершает раунд.
    /// Это закрывает сценарий, когда после истечения времени никто больше не отправляет ответы,
    /// поэтому SubmitAnswer не вызывается и старый вопрос зависает на frontend.
    /// </summary>
    public async Task RequestRoundTimeout(Guid roundId)
    {
        _logger.LogDebug("⏰ [RequestRoundTimeout] Timeout check requested for round {RoundId}", roundId);

        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
        if (round == null)
        {
            _logger.LogDebug(" [RequestRoundTimeout] Round {RoundId} not found", roundId);
            await Clients.Caller.SendAsync("Error", "Round not found");
            return;
        }

        if (round.Status != RoundStatus.Active)
        {
            _logger.LogDebug("ℹ [RequestRoundTimeout] Round {RoundId} is already {Status}", roundId, round.Status);
            return;
        }

        if (!round.IsDeadlinePassed())
        {
            var delay = round.GetDeadline() - DateTime.UtcNow;
            if (delay > TimeSpan.Zero && delay <= TimeSpan.FromSeconds(5))
            {
                _logger.LogDebug("⏳ [RequestRoundTimeout] Deadline has not passed yet. Waiting {DelayMs} ms and rechecking", delay.TotalMilliseconds);
                await Task.Delay(delay.Add(TimeSpan.FromMilliseconds(250)));

                round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
                if (round == null || round.Status != RoundStatus.Active || !round.IsDeadlinePassed())
                    return;
            }
            else
            {
                _logger.LogDebug("⏳ [RequestRoundTimeout] Round {RoundId} deadline has not passed yet", roundId);
                return;
            }
        }

        var session = await _unitOfWork.GameSessions.GetWithDetailsAsync(round.SessionId);
        if (session == null)
        {
            _logger.LogDebug(" [RequestRoundTimeout] Session {SessionId} not found", round.SessionId);
            await Clients.Caller.SendAsync("Error", "Session not found");
            return;
        }

        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(session.RoomId);
        if (room == null)
        {
            _logger.LogDebug(" [RequestRoundTimeout] Room {RoomId} not found", session.RoomId);
            await Clients.Caller.SendAsync("Error", "Room not found");
            return;
        }

        var groupName = GetRoomGroupName(room.Id);
        await AutoEndRoundAndStartNext(round.Id, session, room, groupName, "Time expired");
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
            _hintRevealScheduler.StopHints(roundId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Round {RoundId} ended by owner", roundId);

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
            _logger.LogDebug(ex, "Error ending round {RoundId}", roundId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var connectionId = Context.ConnectionId;
        
        _logger.LogDebug(" [GameHub] CONNECTED - ConnectionId: {ConnectionId}, UserId: {UserId}, Username: {Username}", 
            connectionId, userId, username);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;
        
        _logger.LogDebug(" [GameHub] DISCONNECTED - ConnectionId: {ConnectionId}, UserId: {UserId}", 
            connectionId, userId);
        
        if (exception != null)
        {
            _logger.LogDebug(exception, " [GameHub] Disconnection error for ConnectionId: {ConnectionId}", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    private static string GetRoomGroupName(Guid roomId) => $"{RoomGroupPrefix}{roomId}";
    
    /// <summary>
    /// Конвертирует относительный MediaUrl в полный URL с хостом
    /// </summary>
    private string? ConvertToFullMediaUrl(string? mediaUrl)
    {
        if (string.IsNullOrEmpty(mediaUrl))
            return null;
            
        // Если уже полный URL - возвращаем как есть
        if (mediaUrl.StartsWith("http://") || mediaUrl.StartsWith("https://"))
            return mediaUrl;
            
        // Если относительный - добавляем base URL
        if (mediaUrl.StartsWith("/"))
        {
            var request = Context.GetHttpContext()?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var fullUrl = $"{baseUrl}{mediaUrl}";
                _logger.LogDebug(" [ConvertMediaUrl] {RelativeUrl} → {FullUrl}", mediaUrl, fullUrl);
                return fullUrl;
            }
        }
        
        return mediaUrl;
    }

    private static int GetRevealedHintsCount(GameRound round, int answerTimeMs)
    {
        var elapsedSec = Math.Max(0, answerTimeMs / 1000);
        return round.Question?.Hints?.Count(h => h.RevealTimeSec <= elapsedSec) ?? 0;
    }

    private int CalculateScore(int answerTimeMs, int timeLimitSec, int revealedHintsCount = 0)
    {
        const int baseScore = 100;
        const int timeBonus = 50;

        int score = baseScore;
        var timeLimitMs = timeLimitSec * 1000;
        var speedRatio = 1.0 - ((double)answerTimeMs / timeLimitMs);
        score += (int)(timeBonus * Math.Max(0, speedRatio));

        var hintPenalty = Math.Max(0.6, 1.0 - revealedHintsCount * 0.1);
        return (int)Math.Round(score * hintPenalty);
    }
    
    /// <summary>
    /// Проверяет условия завершения раунда и автоматически завершает если нужно
    /// </summary>
    private async Task CheckAndAutoEndRound(GameRound round, GameSession session, Room room, string groupName)
    {
        try
        {
            // Получаем количество активных игроков из БД (session.Players может быть пустым!)
            _logger.LogDebug(" [AutoEnd] Checking round {RoundId}...", round.Id);
            
            // Загружаем игроков из Scoreboard (они там точно есть если играют)
            var scoreboards = await _unitOfWork.Scoreboards.GetBySessionIdAsync(session.Id);
            var totalPlayers = scoreboards.Count;
            
            _logger.LogDebug(" [AutoEnd] Total players in session: {Total}", totalPlayers);
            
            // ВАЖНО: Если игроков нет - НЕ завершаем раунд!
            if (totalPlayers == 0)
            {
                _logger.LogDebug(" [AutoEnd] No players found in session! Skipping auto-end check.");
                return;
            }
            
            // Подсчитываем игроков с правильными ответами
            var playersWithCorrectAnswers = round.Answers
                .Where(a => a.Evaluation != null && a.Evaluation.IsCorrect)
                .Select(a => a.UserId)
                .Distinct()
                .Count();
                
            _logger.LogDebug(" [AutoEnd] Players answered correctly: {Correct}/{Total}", 
                playersWithCorrectAnswers, totalPlayers);
            
            // Условие 1: Все игроки ответили правильно (и их больше 0!)
            if (playersWithCorrectAnswers >= totalPlayers && totalPlayers > 0)
            {
                _logger.LogDebug(" [AutoEnd] All players answered correctly! Ending round...");
                await AutoEndRoundAndStartNext(round.Id, session, room, groupName, "All players answered");
                return;
            }
            
            // Условие 2: Время истекло (проверяется таймером в клиенте, но можем добавить и тут)
            if (round.IsDeadlinePassed())
            {
                _logger.LogDebug("⏰ [AutoEnd] Time expired! Ending round...");
                await AutoEndRoundAndStartNext(round.Id, session, room, groupName, "Time expired");
                return;
            }
            
            _logger.LogDebug("⏳ [AutoEnd] Round continues: {Correct}/{Total} answered", 
                playersWithCorrectAnswers, totalPlayers);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [AutoEnd] Error checking auto-end conditions");
        }
    }
    
    /// <summary>
    /// Автоматически завершает раунд и запускает следующий
    /// </summary>
    private async Task AutoEndRoundAndStartNext(Guid roundId, GameSession session, Room room, string groupName, string reason)
    {
        try
        {
            _logger.LogDebug(" [AutoEnd] Ending round {RoundId}. Reason: {Reason}", roundId, reason);
            
            var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
            if (round == null || round.Status != RoundStatus.Active)
            {
                _logger.LogDebug(" [AutoEnd] Round {RoundId} not found or not active", roundId);
                return;
            }
            
            // Завершаем раунд
            session.EndRound(roundId);
            _hintRevealScheduler.StopHints(roundId);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogDebug(" [AutoEnd] Round {RoundId} ended", roundId);
            
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
            
            _logger.LogDebug(" [AutoEnd] RoundEnded notification sent to group");
            
            // Пауза 5 секунд перед следующим раундом
            await Task.Delay(5000);
            
            //  Проверяем условия победы
            var roomWithDetails = await _unitOfWork.Rooms.GetWithDetailsAsync(room.Id);
            if (roomWithDetails == null)
            {
                _logger.LogDebug(" [AutoEnd] Room {RoomId} not found for victory check!", room.Id);
                return;
            }
            
            _logger.LogDebug(" [AutoEnd] ========== CHECKING VICTORY CONDITIONS ==========");
            _logger.LogDebug(" [AutoEnd] Victory Type: {VictoryType}", roomWithDetails.VictoryConditionType);
            _logger.LogDebug(" [AutoEnd] Victory Value: {VictoryValue}", roomWithDetails.VictoryValue);
            _logger.LogDebug(" [AutoEnd] Rounds Played: {Played}/{Planned}", session.TotalRoundsPlayed, session.TotalRoundsPlanned);
            
            bool gameFinished = false;
            string finishReason = "";
            
            // Проверка условия 1: По очкам (POINTS)
            if (roomWithDetails.VictoryConditionType == VictoryConditionType.Points)
            {
                var maxScore = scoreboards.Any() ? scoreboards.Max(s => s.ScoreTotal) : 0;
                _logger.LogDebug(" [AutoEnd] Points Mode - Max score: {MaxScore}/{Target}", maxScore, roomWithDetails.VictoryValue);
                
                if (maxScore >= roomWithDetails.VictoryValue)
                {
                    gameFinished = true;
                    finishReason = $"Player reached {roomWithDetails.VictoryValue} points";
                    _logger.LogDebug(" [AutoEnd] Victory by POINTS! Max score {MaxScore} >= target {Target}", maxScore, roomWithDetails.VictoryValue);
                }
                else
                {
                    _logger.LogDebug("⏳ [AutoEnd] Game continues - Max score {MaxScore} < target {Target}", maxScore, roomWithDetails.VictoryValue);
                }
            }
            // Проверка условия 2: По количеству вопросов (QUESTIONS)
            else if (roomWithDetails.VictoryConditionType == VictoryConditionType.Questions)
            {
                _logger.LogDebug(" [AutoEnd] Questions Mode - Played: {Played}/{Target}", session.TotalRoundsPlayed, roomWithDetails.VictoryValue);
                
                if (session.TotalRoundsPlayed >= roomWithDetails.VictoryValue)
                {
                    gameFinished = true;
                    finishReason = $"Completed {roomWithDetails.VictoryValue} questions";
                    _logger.LogDebug(" [AutoEnd] Victory by QUESTIONS! Played {Played} >= target {Target}", session.TotalRoundsPlayed, roomWithDetails.VictoryValue);
                }
                else
                {
                    _logger.LogDebug("⏳ [AutoEnd] Game continues - Played {Played} < target {Target}", session.TotalRoundsPlayed, roomWithDetails.VictoryValue);
                }
            }
            
            // Если игра завершена - отправляем уведомление и выходим
            if (gameFinished)
            {
                _logger.LogDebug(" [AutoEnd] ========== GAME FINISHED ==========");
                _logger.LogDebug(" [AutoEnd] Reason: {Reason}", finishReason);
                
                session.Finish();
                room.FinishGame();
                await _unitOfWork.SaveChangesAsync();
                
                // Определяем победителя
                var winner = scoreboards.FirstOrDefault();
                var winnerInfo = winner != null ? new
                {
                    userId = winner.UserId,
                    username = winner.User.Username,
                    scoreTotal = winner.ScoreTotal,
                    correctCount = winner.CorrectCount
                } : null;
                
                var gameFinishedData = new
                {
                    sessionId = session.Id,
                    reason = finishReason,
                    victoryType = roomWithDetails.VictoryConditionType.ToString(),
                    victoryValue = roomWithDetails.VictoryValue,
                    winner = winnerInfo,
                    finalScoreboard = scoreboards.Select((s, index) => new
                    {
                        rank = index + 1,
                        userId = s.UserId,
                        username = s.User.Username,
                        scoreTotal = s.ScoreTotal,
                        correctCount = s.CorrectCount,
                        isWinner = s.UserId == winner?.UserId
                    }).ToList()
                };
                
                // Отправляем ВСЕМ клиентам
                _logger.LogDebug(" [AutoEnd] Broadcasting GameFinished to ALL clients...");
                _logger.LogDebug(" [AutoEnd] Winner: {Winner} with {Score} points", 
                    winnerInfo?.username ?? "None", winnerInfo?.scoreTotal ?? 0);
                
                await Clients.Group(groupName).SendAsync("GameFinished", gameFinishedData);
                await Clients.All.SendAsync("GameFinished", gameFinishedData);
                
                _logger.LogDebug(" [AutoEnd] Game finished notification sent to all!");
                _logger.LogDebug(" [AutoEnd] ========== END ==========");
                return;
            }
            
            _logger.LogDebug("⏳ [AutoEnd] Game continues - Victory conditions not met yet");
            _logger.LogDebug(" [AutoEnd] ========== VICTORY CHECK COMPLETE ==========");
            
            // Создаем следующий раунд
            _logger.LogDebug(" [AutoEnd] Creating next round...");
            
            //  КРИТИЧНО: Перезагружаем комнату с TagSelections!
            var roomWithTags = await _unitOfWork.Rooms.GetWithDetailsAsync(room.Id);
            if (roomWithTags == null)
            {
                _logger.LogDebug(" [AutoEnd] Room {RoomId} not found!", room.Id);
                await Clients.Group(groupName).SendAsync("Error", "Room not found");
                return;
            }
            
            var tagIds = roomWithTags.TagSelections.Select(ts => ts.TagId).ToArray();
            var difficultyFilters = roomWithTags.GetDifficultyFilters();
            
            _logger.LogDebug(" [AutoEnd] ========== SELECTING NEXT QUESTION ==========");
            _logger.LogDebug(" [AutoEnd] Room: {RoomId} - {RoomName}", roomWithTags.Id, roomWithTags.Name);
            _logger.LogDebug(" [AutoEnd] TagSelections count: {Count}", roomWithTags.TagSelections.Count);
            
            if (roomWithTags.TagSelections.Any())
            {
                _logger.LogDebug(" [AutoEnd] Tag filters ({Count}):", tagIds.Length);
                foreach (var ts in roomWithTags.TagSelections)
                {
                    _logger.LogDebug("    TagId: {TagId}, Weight: {Weight}", ts.TagId, ts.Weight);
                }
            }
            else
            {
                _logger.LogDebug(" [AutoEnd] NO TAG FILTERS (all categories)");
            }
            
            if (difficultyFilters.Any())
            {
                _logger.LogDebug(" [AutoEnd] Difficulty filters: {Filters}", string.Join(", ", difficultyFilters));
            }
            else
            {
                _logger.LogDebug(" [AutoEnd] NO DIFFICULTY FILTERS (all levels)");
            }
            
            
            var question = await _unitOfWork.Questions.GetRandomApprovedWithFiltersAsync(
                tagIds.Any() ? tagIds : null,
                difficultyFilters.Any() ? difficultyFilters : null);
            
            if (question == null)
            {
                _logger.LogDebug(" [AutoEnd] No more questions available with filters!");
                await Clients.Group(groupName).SendAsync("Error", "No more questions available");
                return;
            }
            
            _logger.LogDebug(" [AutoEnd] Question selected: {QuestionId}, Difficulty: {Difficulty}", 
                question.Id, question.Difficulty);
            
            var nextRound = session.AddRound(question.Id, round.TimeLimitSec);
            session.StartRound(nextRound.Id);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogDebug(" [AutoEnd] Next round created: {RoundId}", nextRound.Id);
            
            // Получаем MediaUrl если есть и конвертируем в ПОЛНЫЙ URL
            var nextMediaAsset = question.MediaAssets.FirstOrDefault();
            var nextMediaUrl = ConvertToFullMediaUrl(nextMediaAsset?.Url);
            
            _logger.LogDebug(" [AutoEnd] Question type: {Type}, Media: {HasMedia}", 
                question.Type, nextMediaUrl != null ? "YES" : "NO");
            
            if (nextMediaUrl != null)
            {
                _logger.LogDebug("    Media FULL URL: {Url}", nextMediaUrl);
            }
            
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
                mediaUrl = nextMediaUrl,  //  Отправляем ПОЛНЫЙ MediaUrl!
                hints = Array.Empty<object>()
            };
            
            var roundStartedJson = System.Text.Json.JsonSerializer.Serialize(roundStartedData);
            
            _logger.LogDebug(" [AutoEnd] Sending RoundStarted to group {GroupName}...", groupName);
            _logger.LogDebug("   Data: {Data}", roundStartedJson);
            _logger.LogDebug("   Group members count: checking...");
            
            // Отправляем в группу
            await Clients.Group(groupName).SendAsync("RoundStarted", roundStartedJson);
            _hintRevealScheduler.ScheduleHints(room.Id, nextRound.Id);
            _logger.LogDebug(" [AutoEnd] RoundStarted sent to group");
            
            // ДОПОЛНИТЕЛЬНО: отправляем всем клиентам в Hub (на случай если кто-то не в группе)
            _logger.LogDebug(" [AutoEnd] Also broadcasting to ALL clients in session {SessionId}...", session.Id);
            await Clients.All.SendAsync("RoundStarted", roundStartedJson);
            _logger.LogDebug(" [AutoEnd] RoundStarted broadcast to all");
            
            _logger.LogDebug(" [AutoEnd] Next round started successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [AutoEnd] Error in AutoEndRoundAndStartNext");
        }
    }
}
