using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;

namespace QuizFuzz.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly ILogger<MediaController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IUnitOfWork _unitOfWork;

    public MediaController(
        ILogger<MediaController> logger,
        IWebHostEnvironment environment,
        IUnitOfWork unitOfWork)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Загрузка изображения для вопроса
    /// </summary>
    [HttpPost("upload-image")]
    [Authorize]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        try
        {
            _logger.LogDebug(" [UploadImage] Starting image upload...");

            // Валидация
            if (file == null || file.Length == 0)
            {
                _logger.LogDebug(" [UploadImage] File is empty");
                return BadRequest("File is empty");
            }

            _logger.LogDebug("   File: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            // Проверка типа файла
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogDebug(" [UploadImage] Invalid file type: {Extension}", extension);
                return BadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            // Проверка размера (макс 10 МБ)
            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogDebug(" [UploadImage] File too large: {Size} bytes", file.Length);
                return BadRequest("File size exceeds 10 MB");
            }

            // Создаем папку uploads если не существует
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsPath = Path.Combine(webRootPath, "uploads", "images");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                _logger.LogDebug(" [UploadImage] Created directory: {Path}", uploadsPath);
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            _logger.LogDebug(" [UploadImage] Saving to: {FilePath}", filePath);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Возвращаем полный URL, потому что Blazor WASM обычно открыт на другом origin.
            // Если вернуть только /uploads/..., браузер будет искать файл на origin клиента, а не API.
            var relativeUrl = $"/uploads/images/{fileName}";
            var url = BuildPublicUrl(relativeUrl);
            
            _logger.LogDebug(" [UploadImage] Image uploaded successfully");
            _logger.LogDebug("   Relative URL: {RelativeUrl}", relativeUrl);
            _logger.LogDebug("   Public URL: {Url}", url);

            return Ok(new { url, relativeUrl, fileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [UploadImage] Error uploading image");
            return StatusCode(500, "Error uploading image");
        }
    }

    /// <summary>
    /// Загрузка аудио для вопроса
    /// </summary>
    [HttpPost("upload-audio")]
    [Authorize]
    public async Task<IActionResult> UploadAudio([FromForm] IFormFile file)
    {
        try
        {
            _logger.LogDebug(" [UploadAudio] Starting audio upload...");

            // Валидация
            if (file == null || file.Length == 0)
            {
                _logger.LogDebug(" [UploadAudio] File is empty");
                return BadRequest("File is empty");
            }

            _logger.LogDebug("   File: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            // Проверка типа файла
            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogDebug(" [UploadAudio] Invalid file type: {Extension}", extension);
                return BadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            // Проверка размера (макс 20 МБ)
            if (file.Length > 20 * 1024 * 1024)
            {
                _logger.LogDebug(" [UploadAudio] File too large: {Size} bytes", file.Length);
                return BadRequest("File size exceeds 20 MB");
            }

            // Создаем папку uploads если не существует
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsPath = Path.Combine(webRootPath, "uploads", "audio");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                _logger.LogDebug(" [UploadAudio] Created directory: {Path}", uploadsPath);
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            _logger.LogDebug(" [UploadAudio] Saving to: {FilePath}", filePath);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Возвращаем полный URL, потому что Blazor WASM обычно открыт на другом origin.
            // Если вернуть только /uploads/..., браузер будет искать файл на origin клиента, а не API.
            var relativeUrl = $"/uploads/audio/{fileName}";
            var url = BuildPublicUrl(relativeUrl);
            
            _logger.LogDebug(" [UploadAudio] Audio uploaded successfully");
            _logger.LogDebug("   Relative URL: {RelativeUrl}", relativeUrl);
            _logger.LogDebug("   Public URL: {Url}", url);

            return Ok(new { url, relativeUrl, fileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [UploadAudio] Error uploading audio");
            return StatusCode(500, "Error uploading audio");
        }
    }

    /// <summary>
    /// Удаление медиа файла
    /// </summary>
    [HttpDelete("{fileName}")]
    [Authorize]
    public IActionResult DeleteMedia(string fileName, [FromQuery] string type = "image")
    {
        try
        {
            _logger.LogDebug(" [DeleteMedia] Deleting {Type}: {FileName}", type, fileName);

            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsPath = Path.Combine(webRootPath, "uploads", type == "audio" ? "audio" : "images");
            var filePath = Path.Combine(uploadsPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogDebug(" [DeleteMedia] File not found: {FilePath}", filePath);
                return NotFound("File not found");
            }

            System.IO.File.Delete(filePath);
            
            _logger.LogDebug(" [DeleteMedia] File deleted successfully");

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, " [DeleteMedia] Error deleting media");
            return StatusCode(500, "Error deleting media");
        }
    }
    private string BuildPublicUrl(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return relativeUrl;

        if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _))
            return relativeUrl;

        var request = HttpContext.Request;
        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}{relativeUrl}";
    }

}
