using Microsoft.AspNetCore.Components.Forms;

namespace QuizFuzz.Client.Services.Api;

public interface IMediaApiClient
{
    Task<MediaUploadResult?> UploadImageAsync(IBrowserFile file);
    Task<MediaUploadResult?> UploadAudioAsync(IBrowserFile file);
    Task<MediaUploadResult?> UploadVideoAsync(IBrowserFile file);
}

public class MediaUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
}
