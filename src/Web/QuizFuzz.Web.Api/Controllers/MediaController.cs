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
        ILogger<MediaController> _logger,
        IWebHostEnvironment environment,
        IUnitOfWork unitOfWork)
    {
        _logger = _logger;
        _environment = environment;
        _unitOfWork = unitOfWork;
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
            _logger.LogInformation("📤 [UploadImage] Starting image upload...");
            _logger.LogInformation("   File: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            // Валидация
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("❌ [UploadImage] File is empty");
                return BadRequest("File is empty");
            }

            // Проверка типа файла
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("❌ [UploadImage] Invalid file type: {Extension}", extension);
                return BadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            // Проверка размера (макс 10 МБ)
            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("❌ [UploadImage] File too large: {Size} bytes", file.Length);
                return BadRequest("File size exceeds 10 MB");
            }

            // Создаем папку uploads если не существует
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "images");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                _logger.LogInformation("📁 [UploadImage] Created directory: {Path}", uploadsPath);
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            _logger.LogInformation("💾 [UploadImage] Saving to: {FilePath}", filePath);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Возвращаем URL
            var url = $"/uploads/images/{fileName}";
            
            _logger.LogInformation("✅ [UploadImage] Image uploaded successfully");
            _logger.LogInformation("   URL: {Url}", url);

            return Ok(new { url, fileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UploadImage] Error uploading image");
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
            _logger.LogInformation("📤 [UploadAudio] Starting audio upload...");
            _logger.LogInformation("   File: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            // Валидация
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("❌ [UploadAudio] File is empty");
                return BadRequest("File is empty");
            }

            // Проверка типа файла
            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("❌ [UploadAudio] Invalid file type: {Extension}", extension);
                return BadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            // Проверка размера (макс 20 МБ)
            if (file.Length > 20 * 1024 * 1024)
            {
                _logger.LogWarning("❌ [UploadAudio] File too large: {Size} bytes", file.Length);
                return BadRequest("File size exceeds 20 MB");
            }

            // Создаем папку uploads если не существует
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "audio");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                _logger.LogInformation("📁 [UploadAudio] Created directory: {Path}", uploadsPath);
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            _logger.LogInformation("💾 [UploadAudio] Saving to: {FilePath}", filePath);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Возвращаем URL
            var url = $"/uploads/audio/{fileName}";
            
            _logger.LogInformation("✅ [UploadAudio] Audio uploaded successfully");
            _logger.LogInformation("   URL: {Url}", url);

            return Ok(new { url, fileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UploadAudio] Error uploading audio");
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
            _logger.LogInformation("🗑️ [DeleteMedia] Deleting {Type}: {FileName}", type, fileName);

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", type == "audio" ? "audio" : "images");
            var filePath = Path.Combine(uploadsPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("❌ [DeleteMedia] File not found: {FilePath}", filePath);
                return NotFound("File not found");
            }

            System.IO.File.Delete(filePath);
            
            _logger.LogInformation("✅ [DeleteMedia] File deleted successfully");

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [DeleteMedia] Error deleting media");
            return StatusCode(500, "Error deleting media");
        }
    }
}
