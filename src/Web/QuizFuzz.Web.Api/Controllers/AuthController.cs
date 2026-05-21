using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Domain.ValueObjects;
using QuizFuzz.Shared.Dtos.Auth;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер аутентификации
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return BadRequest("Username must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters");

        // Check if username or email already exists
        if (await _unitOfWork.Users.IsUsernameTakenAsync(request.Username))
            return BadRequest("Username is already taken");

        if (await _unitOfWork.Users.IsEmailTakenAsync(request.Email))
            return BadRequest("Email is already registered");

        try
        {
            // Create email value object
            var email = Email.Create(request.Email);

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // Create user
            var user = new User(request.Username, email, passwordHash);

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate tokens
            var roles = user.Roles.Select(r => r.ToString());
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, email.Value, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _logger.LogDebug("User {Username} registered successfully", request.Username);

            return Ok(new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = email.Value,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Roles = roles.ToList()
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Вход пользователя
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            return BadRequest("Email or username is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required");

        // Try to find user by email or username
        User? user = null;

        if (request.EmailOrUsername.Contains('@'))
        {
            user = await _unitOfWork.Users.GetByEmailAsync(request.EmailOrUsername);
        }
        else
        {
            user = await _unitOfWork.Users.GetByUsernameAsync(request.EmailOrUsername);
        }

        if (user == null)
            return Unauthorized("Invalid credentials");

        // Check if user is banned
        if (user.IsBanActive())
        {
            if (user.BannedUntil.HasValue)
                return Unauthorized($"Account is banned until {user.BannedUntil.Value:yyyy-MM-dd HH:mm}");

            return Unauthorized("Account is permanently banned");
        }

        if (user.IsBanned && user.BannedUntil.HasValue && user.BannedUntil.Value <= DateTime.UtcNow)
        {
            user.Unban();
            await _unitOfWork.SaveChangesAsync();
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        // Update last login
        user.UpdateLastLogin();
        await _unitOfWork.SaveChangesAsync();

        // Generate tokens
        var roles = user.Roles.Select(r => r.ToString());
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email.Value, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _logger.LogDebug("User {Username} logged in successfully", user.Username);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email.Value,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Проверка токена
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken([FromBody] string token)
    {
        var isValid = _tokenService.ValidateToken(token);
        
        if (isValid)
            return Ok(new { valid = true });
        
        return Unauthorized(new { valid = false });
    }

    /// <summary>
    /// Обновить access token используя refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required");

        // В реальном приложении нужно проверить refresh token из БД
        // Здесь упрощенная версия - извлекаем userId из старого access token
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return Unauthorized("Invalid access token");

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token claims");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Unauthorized("User not found");

        if (user.IsBanActive())
            return Unauthorized("User is banned");

        // Генерируем новые токены
        var roles = user.Roles.Select(r => r.ToString());
        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email.Value, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        _logger.LogDebug("Tokens refreshed for user {UserId}", userId);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }

    /// <summary>
    /// Выход из системы (инвалидация токена на клиенте)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogDebug("User {UserId} logged out", userId);

        // В клиентском приложении нужно удалить токены
        // На сервере можно добавить токен в черный список (опционально)
        
        return Ok(new { message = "Logged out successfully" });
    }
}

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
