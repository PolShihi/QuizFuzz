using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Сущность вопроса (Aggregate Root)
/// </summary>
public class Question : BaseEntity, IAggregateRoot
{
    public QuestionType Type { get; private set; }
    public string? Title { get; private set; }
    public string PromptText { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public string LanguageCode { get; private set; }
    public QuestionStatus Status { get; private set; }
    public Guid? AuthorUserId { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User? Author { get; private set; }

    private readonly List<QuestionAnswer> _answers = new();
    public IReadOnlyCollection<QuestionAnswer> Answers => _answers.AsReadOnly();

    private readonly List<Hint> _hints = new();
    public IReadOnlyCollection<Hint> Hints => _hints.AsReadOnly();

    private readonly List<MediaAsset> _mediaAssets = new();
    public IReadOnlyCollection<MediaAsset> MediaAssets => _mediaAssets.AsReadOnly();

    private readonly List<QuestionTag> _tags = new();
    public IReadOnlyCollection<QuestionTag> Tags => _tags.AsReadOnly();

    private Question() { } // EF Core

    public Question(
        QuestionType type,
        string promptText,
        Difficulty difficulty,
        string languageCode = "ru",
        string? title = null,
        Guid? authorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(promptText))
            throw new ArgumentException("Prompt text cannot be empty", nameof(promptText));

        if (promptText.Length < 10)
            throw new ArgumentException("Prompt text must be at least 10 characters", nameof(promptText));

        Type = type;
        Title = title?.Trim();
        PromptText = promptText.Trim();
        Difficulty = difficulty;
        LanguageCode = languageCode;
        Status = QuestionStatus.Draft;
        AuthorUserId = authorUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrompt(string promptText)
    {
        if (string.IsNullOrWhiteSpace(promptText))
            throw new ArgumentException("Prompt text cannot be empty", nameof(promptText));

        if (promptText.Length < 10)
            throw new ArgumentException("Prompt text must be at least 10 characters", nameof(promptText));

        PromptText = promptText.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string? title)
    {
        Title = title?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDifficulty(Difficulty difficulty)
    {
        Difficulty = difficulty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubmitForReview()
    {
        if (Status != QuestionStatus.Draft && Status != QuestionStatus.Rejected)
            throw new InvalidOperationException("Only draft or rejected questions can be submitted for review");

        if (!_answers.Any(a => a.IsPrimary))
            throw new InvalidOperationException("Question must have at least one primary answer");

        Status = QuestionStatus.UnderReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != QuestionStatus.UnderReview)
            throw new InvalidOperationException("Only questions under review can be approved");

        Status = QuestionStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != QuestionStatus.UnderReview)
            throw new InvalidOperationException("Only questions under review can be rejected");

        Status = QuestionStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = QuestionStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public QuestionAnswer AddAnswer(string answerText, bool isPrimary = false, string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        var answer = new QuestionAnswer(Id, answerText, isPrimary, languageCode ?? LanguageCode);
        _answers.Add(answer);
        UpdatedAt = DateTime.UtcNow;

        return answer;
    }

    public void RemoveAnswer(Guid answerId)
    {
        var answer = _answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null)
            throw new InvalidOperationException("Answer not found");

        if (answer.IsPrimary && _answers.Count(a => a.IsPrimary) == 1)
            throw new InvalidOperationException("Cannot remove the last primary answer");

        _answers.Remove(answer);
        UpdatedAt = DateTime.UtcNow;
    }

    public Hint AddHint(int orderIndex, string hintText, int revealTimeSec)
    {
        var hint = new Hint(Id, orderIndex, hintText, revealTimeSec);
        _hints.Add(hint);
        UpdatedAt = DateTime.UtcNow;

        return hint;
    }

    public void RemoveHint(Guid hintId)
    {
        var hint = _hints.FirstOrDefault(h => h.Id == hintId);
        if (hint != null)
        {
            _hints.Remove(hint);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public MediaAsset AddMediaAsset(string mediaType, string url, string storageProvider = "S3")
    {
        var asset = new MediaAsset(Id, mediaType, url, storageProvider);
        _mediaAssets.Add(asset);
        UpdatedAt = DateTime.UtcNow;

        return asset;
    }

    public void AddTag(Tag tag)
    {
        if (tag == null)
            throw new ArgumentNullException(nameof(tag));

        if (_tags.Any(qt => qt.TagId == tag.Id))
            return; // Already exists

        var questionTag = new QuestionTag(Id, tag.Id);
        _tags.Add(questionTag);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTag(Guid tagId)
    {
        var questionTag = _tags.FirstOrDefault(qt => qt.TagId == tagId);
        if (questionTag != null)
        {
            _tags.Remove(questionTag);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
