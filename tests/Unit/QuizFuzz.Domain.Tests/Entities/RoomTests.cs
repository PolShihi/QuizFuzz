using FluentAssertions;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Tests.Entities;

public class RoomTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateRoom()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var name = "Test Room";
        var visibility = RoomVisibility.Public;
        var maxPlayers = 10;
        var victoryType = VictoryConditionType.Points;
        var victoryValue = 100;
        var tagMode = TagSelectionMode.Any;

        // Act
        var room = new Room(ownerId, name, visibility, maxPlayers, victoryType, victoryValue, tagMode);

        // Assert
        room.OwnerUserId.Should().Be(ownerId);
        room.Name.Should().Be(name);
        room.Visibility.Should().Be(visibility);
        room.MaxPlayers.Should().Be(maxPlayers);
        room.VictoryConditionType.Should().Be(victoryType);
        room.VictoryValue.Should().Be(victoryValue);
        room.TagSelectionMode.Should().Be(tagMode);
        room.Status.Should().Be(RoomStatus.Lobby);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ab")] // Too short
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act
        Action act = () => new Room(
            Guid.NewGuid(),
            invalidName,
            RoomVisibility.Public,
            10,
            VictoryConditionType.Points,
            100,
            TagSelectionMode.Any
        );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(51)] // Too many
    public void Constructor_WithInvalidMaxPlayers_ShouldThrowArgumentException(int invalidMaxPlayers)
    {
        // Act
        Action act = () => new Room(
            Guid.NewGuid(),
            "Test Room",
            RoomVisibility.Public,
            invalidMaxPlayers,
            VictoryConditionType.Points,
            100,
            TagSelectionMode.Any
        );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidVictoryValue_ShouldThrowArgumentException(int invalidValue)
    {
        // Act
        Action act = () => new Room(
            Guid.NewGuid(),
            "Test Room",
            RoomVisibility.Public,
            10,
            VictoryConditionType.Points,
            invalidValue,
            TagSelectionMode.Any
        );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTagSelection_ShouldAddTagToRoom()
    {
        // Arrange
        var room = CreateTestRoom();
        var tagId = Guid.NewGuid();

        // Act
        room.AddTagSelection(tagId, 1);

        // Assert
        room.TagSelections.Should().HaveCount(1);
        room.TagSelections.First().TagId.Should().Be(tagId);
    }

    [Fact]
    public void RemoveTagSelection_ShouldRemoveTagFromRoom()
    {
        // Arrange
        var room = CreateTestRoom();
        var tagId = Guid.NewGuid();
        room.AddTagSelection(tagId, 1);

        // Act
        room.RemoveTagSelection(tagId);

        // Assert
        room.TagSelections.Should().BeEmpty();
    }

    [Fact]
    public void SetAccessCode_WithValidHash_ShouldSetAccessCode()
    {
        // Arrange
        var room = CreateTestRoom();
        var codeHash = "hashedcode";

        // Act
        room.SetAccessCode(codeHash);

        // Assert
        room.AccessCodeHash.Should().Be(codeHash);
    }

    [Fact]
    public void StartGame_WhenInLobby_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var room = CreateTestRoom();

        // Act
        room.StartGame();

        // Assert
        room.Status.Should().Be(RoomStatus.InProgress);
    }

    [Fact]
    public void StartGame_WhenNotInLobby_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var room = CreateTestRoom();
        room.StartGame();

        // Act
        Action act = () => room.StartGame();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FinishGame_ShouldChangeStatusToFinished()
    {
        // Arrange
        var room = CreateTestRoom();
        room.StartGame();

        // Act
        room.FinishGame();

        // Assert
        room.Status.Should().Be(RoomStatus.Finished);
    }

    // Removed Archive test - method doesn't exist in current implementation

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var room = CreateTestRoom();
        var newName = "Updated Room Name";

        // Act
        room.UpdateName(newName);

        // Assert
        room.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateName_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var room = CreateTestRoom();

        // Act
        Action act = () => room.UpdateName(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateMaxPlayers_WithValidNumber_ShouldUpdateMaxPlayers()
    {
        // Arrange
        var room = CreateTestRoom();

        // Act
        room.UpdateMaxPlayers(20);

        // Assert
        room.MaxPlayers.Should().Be(20);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(51)]
    public void UpdateMaxPlayers_WithInvalidNumber_ShouldThrowArgumentException(int invalidMax)
    {
        // Arrange
        var room = CreateTestRoom();

        // Act
        Action act = () => room.UpdateMaxPlayers(invalidMax);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static Room CreateTestRoom()
    {
        return new Room(
            Guid.NewGuid(),
            "Test Room",
            RoomVisibility.Public,
            10,
            VictoryConditionType.Points,
            100,
            TagSelectionMode.Any
        );
    }
}
