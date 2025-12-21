using FluentAssertions;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Tests.Entities;

public class GameSessionTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateGameSession()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var totalRounds = 10;

        // Act
        var session = new GameSession(roomId, totalRounds);

        // Assert
        session.RoomId.Should().Be(roomId);
        session.TotalRoundsPlanned.Should().Be(totalRounds);
        session.Status.Should().Be(GameSessionStatus.Pending);
        session.TotalRoundsPlayed.Should().Be(0);
        session.CurrentRoundId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidTotalRounds_ShouldThrowArgumentException(int invalidRounds)
    {
        // Arrange
        var roomId = Guid.NewGuid();

        // Act
        Action act = () => new GameSession(roomId, invalidRounds);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddPlayer_ShouldAddPlayerToSession()
    {
        // Arrange
        var session = CreateTestSession();
        var userId = Guid.NewGuid();

        // Act
        session.AddPlayer(userId, false);

        // Assert
        session.Players.Should().HaveCount(1);
        session.Players.First().UserId.Should().Be(userId);
    }

    [Fact]
    public void RemovePlayer_ShouldMarkPlayerAsInactive()
    {
        // Arrange
        var session = CreateTestSession();
        var userId = Guid.NewGuid();
        session.AddPlayer(userId, false);

        // Act
        session.RemovePlayer(userId);

        // Assert
        var player = session.Players.First(p => p.UserId == userId);
        player.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Start_WhenPending_ShouldChangeStatusToActive()
    {
        // Arrange
        var session = CreateTestSession();

        // Act
        session.Start();

        // Assert
        session.Status.Should().Be(GameSessionStatus.Active);
        session.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_WhenAlreadyActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateTestSession();
        session.Start();

        // Act
        Action act = () => session.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddRound_ShouldAddRoundToSession()
    {
        // Arrange
        var session = CreateTestSession();
        session.Start(); // Must start session first
        var questionId = Guid.NewGuid();
        var timeLimit = 60;

        // Act
        var round = session.AddRound(questionId, timeLimit);

        // Assert
        session.Rounds.Should().Contain(round);
        round.QuestionId.Should().Be(questionId);
        round.TimeLimitSec.Should().Be(timeLimit);
        round.RoundIndex.Should().Be(0);
    }

    [Fact]
    public void StartRound_ShouldSetCurrentRound()
    {
        // Arrange
        var session = CreateTestSession();
        session.Start();
        var round = session.AddRound(Guid.NewGuid(), 60);

        // Act
        session.StartRound(round.Id);

        // Assert
        session.CurrentRoundId.Should().Be(round.Id);
        round.Status.Should().Be(RoundStatus.Active);
    }

    [Fact]
    public void EndRound_ShouldIncrementPlayedRounds()
    {
        // Arrange
        var session = CreateTestSession();
        session.Start();
        var round = session.AddRound(Guid.NewGuid(), 60);
        session.StartRound(round.Id);

        // Act
        session.EndRound(round.Id);

        // Assert
        session.TotalRoundsPlayed.Should().Be(1);
        session.CurrentRoundId.Should().BeNull();
        round.Status.Should().Be(RoundStatus.Ended);
        round.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public void Finish_ShouldChangeStatusToFinished()
    {
        // Arrange
        var session = CreateTestSession();
        session.Start();

        // Act
        session.Finish();

        // Assert
        session.Status.Should().Be(GameSessionStatus.Finished);
        session.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public void Abort_ShouldChangeStatusToAborted()
    {
        // Arrange
        var session = CreateTestSession();
        session.Start();

        // Act
        session.Abort();

        // Assert
        session.Status.Should().Be(GameSessionStatus.Aborted);
        session.EndedAt.Should().NotBeNull();
    }

    private static GameSession CreateTestSession()
    {
        return new GameSession(Guid.NewGuid(), 10);
    }
}
