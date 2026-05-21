using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace QuizFuzz.Client.Services.Api;

public class MediaApiClient : IMediaApiClient
{
    private const long MaxImageSize = 10 * 1024 * 1024;
    private const long MaxAudioSize = 20 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly ILogger<MediaApiClient> _logger;

    public MediaApiClient(HttpClient httpClient, ILogger<MediaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<MediaUploadResult?> UploadImageAsync(IBrowserFile file)
    {
        return UploadAsync("api/media/upload-image", file, MaxImageSize);
    }

    public Task<MediaUploadResult?> UploadAudioAsync(IBrowserFile file)
    {
        return UploadAsync("api/media/upload-audio", file, MaxAudioSize);
    }

    private async Task<MediaUploadResult?> UploadAsync(string endpoint, IBrowserFile file, long maxAllowedSize)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream(maxAllowedSize);
            using var fileContent = new StreamContent(stream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            }

            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync(endpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogDebug(" [MediaApiClient] Upload failed. Endpoint: {Endpoint}, Status: {Status}, Error: {Error}",
                    endpoint, response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MediaUploadResult>();
            if (result is not null)
            {
                result.Url = ToAbsoluteMediaUrl(result.Url);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [MediaApiClient] Error uploading media to {Endpoint}", endpoint);
            return null;
        }
    }

    private string ToAbsoluteMediaUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        if (Uri.TryCreate(url, UriKind.Absolute, out _))
            return url;

        if (_httpClient.BaseAddress is null)
            return url;

        return new Uri(_httpClient.BaseAddress, url.TrimStart('/')).ToString();
    }
}
