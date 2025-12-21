using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Application.Game.Commands.SubmitAnswer;
using QuizFuzz.Application.Game.Queries.GetScoreboard;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер для игровой логики
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuzzyMatchingService _fuzzyMatchingService;
    private readonly ILogger<GameController> _logger;

    public GameController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFuzzyMatchingService fuzzyMatchingService,
        ILogger<GameController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fuzzyMatchingService = fuzzyMatchingService;
        _logger = logger;
    }

    /// <summary>
    /// Получить информацию о сессии
    /// </summary>
    [HttpGet("session/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var session = await _unitOfWork.GameSessions.GetWithDetailsAsync(id);

        if (session == null)
            return NotFound($"Session with ID {id} not found");

        var result = new
        {
            session.Id,
            session.RoomId,
            RoomName = session.Room.Name,
            session.Status,
            session.StartedAt,
            session.EndedAt,
            session.TotalRoundsPlanned,
            session.TotalRoundsPlayed,
            session.CurrentRoundId,
            Players = session.Players.Where(p => p.IsActive).Select(p => new
            {
                p.UserId,
                Username = p.User.Username,
                p.IsOwnerSnapshot,
                p.JoinedAt
            }),
            Rounds = session.Rounds.Select(r => new
            {
                r.Id,
                r.RoundIndex,
                r.QuestionId,
                r.Status,
                r.StartedAt,
                r.EndedAt,
                r.WinnerUserId
            })
        };

        return Ok(result);
    }

    /// <summary>
    /// Начать новый раунд
    /// </summary>
    [HttpPost("session/{sessionId}/start-round")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartRound(Guid sessionId)
    {
        var session = await _unitOfWork.GameSessions.GetWithDetailsAsync(sessionId);

        if (session == null)
            return NotFound($"Session with ID {sessionId} not found");

        if (session.Status != GameSessionStatus.Active)
            return BadRequest("Session is not active");

        if (session.CurrentRoundId.HasValue)
            return BadRequest("A round is already in progress");

        try
        {
            // Получаем случайный вопрос по тегам комнаты
            var room = session.Room;
            var tagIds = room.TagSelections.Select(ts => ts.TagId).ToArray();
            
            var question = await _unitOfWork.Questions.GetRandomApprovedAsync(tagIds.Any() ? tagIds : null);

            if (question == null)
                return BadRequest("No approved questions available");

            // Создаем новый раунд
            var round = session.AddRound(question.Id, 60); // 60 секунд по умолчанию
            session.StartRound(round.Id);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Round {RoundId} started in session {SessionId}", round.Id, sessionId);

            return Ok(new
            {
                roundId = round.Id,
                questionId = question.Id,
                promptText = question.PromptText,
                title = question.Title,
                difficulty = question.Difficulty,
                timeLimitSec = round.TimeLimitSec,
                startedAt = round.StartedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting round for session {SessionId}", sessionId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Отправить ответ на вопрос
    /// </summary>
    [HttpPost("answer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerCommand command)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(command.RoundId);

        if (round == null)
            return NotFound($"Round with ID {command.RoundId} not found");

        if (round.Status != RoundStatus.Active)
            return BadRequest("Round is not active");

        if (round.IsDeadlinePassed())
            return BadRequest("Round time limit has expired");

        // Проверяем, не отвечал ли уже пользователь
        var existingAnswer = round.Answers.FirstOrDefault(a => a.UserId == userId.Value);
        if (existingAnswer != null)
            return BadRequest("You have already submitted an answer for this round");

        try
        {
            // Создаем ответ игрока
            var answerTimeMs = round.GetElapsedTimeMs();
            var playerAnswer = round.AddAnswer(userId.Value, command.AnswerText, answerTimeMs);

            await _unitOfWork.SaveChangesAsync();

            // Оцениваем ответ с помощью Fuzzy Matching
            var matchResult = await _fuzzyMatchingService.EvaluateAnswerAsync(
                round.QuestionId,
                command.AnswerText);

            // Создаем оценку
            var confidence = Confidence.Create(matchResult.Confidence);
            var evaluation = new AnswerEvaluation(
                playerAnswer.Id,
                matchResult.IsCorrect,
                matchResult.Strategy,
                matchResult.IsCorrect ? CalculateScore(answerTimeMs, round.TimeLimitSec) : 0,
                confidence,
                matchResult.NormalizedAnswer,
                matchResult.MatchedQuestionAnswerId,
                matchResult.MatchedAliasId);

            playerAnswer.SetEvaluation(evaluation);

            // Обновляем scoreboard
            var scoreboard = await _unitOfWork.Scoreboards.GetBySessionAndUserAsync(round.SessionId, userId.Value);
            
            if (scoreboard != null)
            {
                var isFirstCorrect = matchResult.IsCorrect && !round.Answers.Any(a => 
                    a.UserId != userId.Value && 
                    a.Evaluation != null && 
                    a.Evaluation.IsCorrect);

                scoreboard.AddScore(evaluation.ScoreAwarded, matchResult.IsCorrect, isFirstCorrect);

                // Устанавливаем победителя раунда если это первый правильный ответ
                if (isFirstCorrect)
                {
                    round.SetWinner(userId.Value);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Answer submitted for round {RoundId} by user {UserId}: {IsCorrect}",
                command.RoundId, userId, matchResult.IsCorrect);

            var result = new SubmitAnswerResult
            {
                PlayerAnswerId = playerAnswer.Id,
                IsCorrect = matchResult.IsCorrect,
                Strategy = matchResult.Strategy,
                ScoreAwarded = evaluation.ScoreAwarded,
                Confidence = matchResult.Confidence,
                IsFirstCorrect = evaluation.ScoreAwarded > 0 && matchResult.IsCorrect,
                NewTotalScore = scoreboard?.ScoreTotal ?? 0,
                AnswerTimeMs = answerTimeMs
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for round {RoundId}", command.RoundId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Завершить раунд
    /// </summary>
    [HttpPost("round/{roundId}/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EndRound(Guid roundId)
    {
        var round = await _unitOfWork.GameRounds.GetWithDetailsAsync(roundId);

        if (round == null)
            return NotFound($"Round with ID {roundId} not found");

        if (round.Status != RoundStatus.Active)
            return BadRequest("Round is not active");

        try
        {
            var session = await _unitOfWork.GameSessions.GetByIdAsync(round.SessionId);
            if (session == null)
                return BadRequest("Session not found");

            session.EndRound(roundId);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Round {RoundId} ended", roundId);

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
                    answerTimeMs = a.AnswerTimeMs
                });

            return Ok(new
            {
                roundId,
                winnerId = round.WinnerUserId,
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending round {RoundId}", roundId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить scoreboard сессии
    /// </summary>
    [HttpGet("scoreboard/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScoreboard(Guid sessionId)
    {
        var scoreboards = await _unitOfWork.Scoreboards.GetLeaderboardAsync(sessionId, 10);

        if (!scoreboards.Any())
            return NotFound("No scoreboard data found for this session");

        var result = new ScoreboardDto
        {
            SessionId = sessionId,
            Players = scoreboards.Select((s, index) => new PlayerScoreDto
            {
                UserId = s.UserId,
                Username = s.User.Username,
                ScoreTotal = s.ScoreTotal,
                CorrectCount = s.CorrectCount,
                UniqueCorrectCount = s.UniqueCorrectCount,
                Rank = index + 1
            }).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Получить текущий активный раунд сессии
    /// </summary>
    [HttpGet("session/{sessionId}/current-round")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentRound(Guid sessionId)
    {
        var round = await _unitOfWork.GameRounds.GetActiveRoundBySessionIdAsync(sessionId);

        if (round == null)
            return NotFound("No active round found for this session");

        var result = new
        {
            round.Id,
            round.RoundIndex,
            round.QuestionId,
            questionText = round.Question.PromptText,
            questionTitle = round.Question.Title,
            difficulty = round.Question.Difficulty,
            round.Status,
            round.StartedAt,
            timeLimitSec = round.TimeLimitSec,
            timeRemainingMs = Math.Max(0, (int)(round.GetDeadline() - DateTime.UtcNow).TotalMilliseconds),
            hints = round.Question.Hints.OrderBy(h => h.OrderIndex).Select(h => new
            {
                h.OrderIndex,
                h.HintText,
                h.RevealTimeSec
            }),
            answersCount = round.Answers.Count
        };

        return Ok(result);
    }

    private int CalculateScore(int answerTimeMs, int timeLimitSec)
    {
        const int baseScore = 100;
        const int timeBonus = 50;

        // Базовые очки за правильный ответ
        int score = baseScore;

        // Бонус за скорость (максимум 50 очков)
        var timeLimitMs = timeLimitSec * 1000;
        var speedRatio = 1.0 - ((double)answerTimeMs / timeLimitMs);
        score += (int)(timeBonus * Math.Max(0, speedRatio));

        return score;
    }
}
