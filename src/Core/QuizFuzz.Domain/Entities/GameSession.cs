using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Игровая сессия (Aggregate Root)
/// </summary>
public class GameSession : BaseEntity, IAggregateRoot
{
    public Guid RoomId { get; private set; }
    public GameSessionStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public int TotalRoundsPlanned { get; private set; }
    public int TotalRoundsPlayed { get; private set; }
    public Guid? CurrentRoundId { get; private set; }

    // Navigation properties
    public Room Room { get; private set; } = null!;

    private readonly List<GameRound> _rounds = new();
    public IReadOnlyCollection<GameRound> Rounds => _rounds.AsReadOnly();

    private readonly List<PlayerInRoom> _players = new();
    public IReadOnlyCollection<PlayerInRoom> Players => _players.AsReadOnly();

    private readonly List<Scoreboard> _scoreboards = new();
    public IReadOnlyCollection<Scoreboard> Scoreboards => _scoreboards.AsReadOnly();

    private GameSession() { } // EF Core

    public GameSession(Guid roomId, int totalRoundsPlanned)
    {
        if (totalRoundsPlanned <= 0)
            throw new ArgumentException("Total rounds planned must be positive", nameof(totalRoundsPlanned));

        RoomId = roomId;
        Status = GameSessionStatus.Pending;
        TotalRoundsPlanned = totalRoundsPlanned;
        TotalRoundsPlayed = 0;
    }

    public void Start()
    {
        if (Status != GameSessionStatus.Pending)
            throw new InvalidOperationException("Can only start a pending session");

        Status = GameSessionStatus.Active;
        StartedAt = DateTime.UtcNow;
    }

    public void Finish()
    {
        if (Status != GameSessionStatus.Active)
            throw new InvalidOperationException("Can only finish an active session");

        Status = GameSessionStatus.Finished;
        EndedAt = DateTime.UtcNow;
    }

    public void Abort()
    {
        if (Status == GameSessionStatus.Finished)
            throw new InvalidOperationException("Cannot abort a finished session");

        Status = GameSessionStatus.Aborted;
        EndedAt = DateTime.UtcNow;
    }

    public GameRound AddRound(Guid questionId, int timeLimitSec = 60)
    {
        if (Status != GameSessionStatus.Active)
            throw new InvalidOperationException("Can only add rounds to an active session");

        var roundIndex = TotalRoundsPlayed;
        var round = new GameRound(Id, roundIndex, questionId, timeLimitSec);
        _rounds.Add(round);

        return round;
    }

    public void StartRound(Guid roundId)
    {
        var round = _rounds.FirstOrDefault(r => r.Id == roundId);
        if (round == null)
            throw new InvalidOperationException("Round not found");

        if (CurrentRoundId.HasValue)
            throw new InvalidOperationException("Another round is already active");

        round.Start();
        CurrentRoundId = roundId;
    }

    public void EndRound(Guid roundId)
    {
        var round = _rounds.FirstOrDefault(r => r.Id == roundId);
        if (round == null)
            throw new InvalidOperationException("Round not found");

        if (CurrentRoundId != roundId)
            throw new InvalidOperationException("This round is not currently active");

        round.End();
        CurrentRoundId = null;
        TotalRoundsPlayed++;
    }

    public PlayerInRoom AddPlayer(Guid userId, bool isOwner = false)
    {
        if (_players.Any(p => p.UserId == userId && p.LeftAt == null))
            throw new InvalidOperationException("Player is already in the session");

        var player = new PlayerInRoom(Id, userId, isOwner);
        _players.Add(player);

        // Initialize scoreboard for the player
        var scoreboard = new Scoreboard(Id, userId);
        _scoreboards.Add(scoreboard);

        return player;
    }

    public void RemovePlayer(Guid userId)
    {
        var player = _players.FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);
        if (player != null)
        {
            player.Leave();
        }
    }

    public Scoreboard GetScoreboard(Guid userId)
    {
        var scoreboard = _scoreboards.FirstOrDefault(s => s.UserId == userId);
        if (scoreboard == null)
            throw new InvalidOperationException("Scoreboard not found for user");

        return scoreboard;
    }
}
