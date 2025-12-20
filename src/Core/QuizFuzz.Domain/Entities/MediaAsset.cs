using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Медиа-ресурс (изображение, аудио, видео)
/// </summary>
public class MediaAsset : BaseEntity
{
    public Guid QuestionId { get; private set; }
    public string MediaType { get; private set; } // IMAGE, AUDIO, VIDEO
    public string Url { get; private set; }
    public string StorageProvider { get; private set; }
    public string? Checksum { get; private set; }
    public int? DurationSec { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public bool IsSensitive { get; private set; }

    // Navigation properties
    public Question Question { get; private set; } = null!;

    private MediaAsset() { } // EF Core

    public MediaAsset(
        Guid questionId,
        string mediaType,
        string url,
        string storageProvider = "S3")
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("Media type cannot be empty", nameof(mediaType));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        QuestionId = questionId;
        MediaType = mediaType.ToUpperInvariant();
        Url = url;
        StorageProvider = storageProvider;
        IsSensitive = false;
    }

    public void UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        Url = url;
    }

    public void SetChecksum(string checksum)
    {
        Checksum = checksum;
    }

    public void SetDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Invalid dimensions");

        Width = width;
        Height = height;
    }

    public void SetDuration(int durationSec)
    {
        if (durationSec <= 0)
            throw new ArgumentException("Duration must be positive", nameof(durationSec));

        DurationSec = durationSec;
    }

    public void MarkAsSensitive()
    {
        IsSensitive = true;
    }

    public void UnmarkAsSensitive()
    {
        IsSensitive = false;
    }
}
