using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Игровая комната (Aggregate Root)
/// </summary>
public class Room : BaseEntity, IAggregateRoot
{
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; }
    public RoomVisibility Visibility { get; private set; }
    public string? AccessCodeHash { get; private set; }
    public int MaxPlayers { get; private set; }
    public VictoryConditionType VictoryConditionType { get; private set; }
    public int VictoryValue { get; private set; }
    public TagSelectionMode TagSelectionMode { get; private set; }
    public RoomStatus Status { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User Owner { get; private set; } = null!;

    private readonly List<GameSession> _sessions = new();
    public IReadOnlyCollection<GameSession> Sessions => _sessions.AsReadOnly();

    private readonly List<RoomTagSelection> _tagSelections = new();
    public IReadOnlyCollection<RoomTagSelection> TagSelections => _tagSelections.AsReadOnly();

    private readonly List<Invitation> _invitations = new();
    public IReadOnlyCollection<Invitation> Invitations => _invitations.AsReadOnly();

    private Room() { } // EF Core

    public Room(
        Guid ownerUserId,
        string name,
        RoomVisibility visibility,
        int maxPlayers = 10,
        VictoryConditionType victoryConditionType = VictoryConditionType.Points,
        int victoryValue = 1000,
        TagSelectionMode tagSelectionMode = TagSelectionMode.Any)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name cannot be empty", nameof(name));

        if (name.Length < 3)
            throw new ArgumentException("Room name must be at least 3 characters", nameof(name));

        if (maxPlayers < 2 || maxPlayers > 50)
            throw new ArgumentException("Max players must be between 2 and 50", nameof(maxPlayers));

        if (victoryValue <= 0)
            throw new ArgumentException("Victory value must be positive", nameof(victoryValue));

        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Visibility = visibility;
        MaxPlayers = maxPlayers;
        VictoryConditionType = victoryConditionType;
        VictoryValue = victoryValue;
        TagSelectionMode = tagSelectionMode;
        Status = RoomStatus.Lobby;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name cannot be empty", nameof(name));

        if (name.Length < 3)
            throw new ArgumentException("Room name must be at least 3 characters", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAccessCode(string? accessCodeHash)
    {
        if (Visibility == RoomVisibility.Private && string.IsNullOrWhiteSpace(accessCodeHash))
            throw new ArgumentException("Private room must have an access code", nameof(accessCodeHash));

        AccessCodeHash = accessCodeHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMaxPlayers(int maxPlayers)
    {
        if (maxPlayers < 2 || maxPlayers > 50)
            throw new ArgumentException("Max players must be between 2 and 50", nameof(maxPlayers));

        MaxPlayers = maxPlayers;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartGame()
    {
        if (Status != RoomStatus.Lobby)
            throw new InvalidOperationException("Can only start game from lobby");

        Status = RoomStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FinishGame()
    {
        if (Status != RoomStatus.InProgress)
            throw new InvalidOperationException("Can only finish a game that is in progress");

        Status = RoomStatus.Finished;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReturnToLobby()
    {
        Status = RoomStatus.Lobby;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTagSelection(Guid tagId, int weight = 1)
    {
        if (_tagSelections.Any(ts => ts.TagId == tagId))
            return; // Already exists

        var selection = new RoomTagSelection(Id, tagId, weight);
        _tagSelections.Add(selection);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTagSelection(Guid tagId)
    {
        var selection = _tagSelections.FirstOrDefault(ts => ts.TagId == tagId);
        if (selection != null)
        {
            _tagSelections.Remove(selection);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public Invitation CreateInvitation(string code, DateTime expiresAt, Guid createdByUserId)
    {
        var invitation = new Invitation(Id, code, expiresAt, createdByUserId);
        _invitations.Add(invitation);
        UpdatedAt = DateTime.UtcNow;

        return invitation;
    }
}
