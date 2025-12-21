using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
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
    /// Присоединиться к игровой комнате
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

        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);
        if (round == null)
        {
            await Clients.Caller.SendAsync("Error", "Round not found");
            return;
        }

        if (round.Status != RoundStatus.Active)
        {
            await Clients.Caller.SendAsync("Error", "Round is not active");
            return;
        }

        if (round.IsDeadlinePassed())
        {
            await Clients.Caller.SendAsync("Error", "Time limit expired");
            return;
        }

        // Проверяем повторный ответ
        if (round.Answers.Any(a => a.UserId == userId))
        {
            await Clients.Caller.SendAsync("Error", "You have already answered");
            return;
        }

        try
        {
            var answerTimeMs = round.GetElapsedTimeMs();
            var playerAnswer = round.AddAnswer(userId, answerText, answerTimeMs);
            await _unitOfWork.SaveChangesAsync();

            // Оцениваем ответ
            var matchResult = await _fuzzyMatchingService.EvaluateAnswerAsync(
                round.QuestionId,
                answerText);

            var confidence = Domain.ValueObjects.Confidence.Create(matchResult.Confidence);
            var scoreAwarded = matchResult.IsCorrect ? CalculateScore(answerTimeMs, round.TimeLimitSec) : 0;

            var evaluation = new Domain.Entities.AnswerEvaluation(
                playerAnswer.Id,
                matchResult.IsCorrect,
                matchResult.Strategy,
                scoreAwarded,
                confidence,
                matchResult.NormalizedAnswer,
                matchResult.MatchedQuestionAnswerId,
                matchResult.MatchedAliasId);

            playerAnswer.SetEvaluation(evaluation);

            // Обновляем scoreboard
            var scoreboard = await _unitOfWork.Scoreboards.GetBySessionAndUserAsync(round.SessionId, userId);
            var isFirstCorrect = false;

            if (scoreboard != null)
            {
                isFirstCorrect = matchResult.IsCorrect && !round.Answers.Any(a =>
                    a.UserId != userId &&
                    a.Evaluation != null &&
                    a.Evaluation.IsCorrect);

                scoreboard.AddScore(scoreAwarded, matchResult.IsCorrect, isFirstCorrect);

                if (isFirstCorrect)
                {
                    round.SetWinner(userId);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Answer submitted in round {RoundId} by {Username}: {IsCorrect}",
                roundId, username, matchResult.IsCorrect);

            var session = await _unitOfWork.GameSessions.GetByIdAsync(round.SessionId);
            var room = session!.Room;
            var groupName = GetRoomGroupName(room.Id);

            // Отправляем результат игроку
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

            // Уведомляем всех о том, что кто-то ответил (без деталей)
            await Clients.Group(groupName).SendAsync("PlayerAnswered", new
            {
                userId,
                username,
                timestamp = DateTime.UtcNow,
                answeredCount = round.Answers.Count,
                isCorrect = matchResult.IsCorrect
            });

            // Если это первый правильный ответ, уведомляем всех
            if (isFirstCorrect)
            {
                await Clients.Group(groupName).SendAsync("FirstCorrectAnswer", new
                {
                    userId,
                    username,
                    scoreAwarded,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for round {RoundId}", roundId);
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected from GameHub", userId);

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
}
