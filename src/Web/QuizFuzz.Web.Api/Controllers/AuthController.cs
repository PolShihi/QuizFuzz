using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.ValueObjects;
using QuizFuzz.Shared.Dtos.Auth;
using System.Security.Cryptography;
using System.Text;

namespace QuizFuzz.Web.Api.Controllers;

/// <summary>
/// Контроллер аутентификации
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const int VerificationCodeLength = 6;
    private const int MaxVerificationAttempts = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthController> _logger;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly int _verificationCodeExpirationMinutes;
    private readonly int _verificationCodeResendCooldownSeconds;
    private readonly int _maxVerificationCodeResends;
    private readonly string _verificationSecret;

    public AuthController(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _logger = logger;
        _accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        _verificationCodeExpirationMinutes = int.Parse(configuration["EmailVerification:CodeExpirationMinutes"] ?? "10");
        _verificationCodeResendCooldownSeconds = int.Parse(configuration["EmailVerification:ResendCooldownSeconds"] ?? "60");
        _maxVerificationCodeResends = int.Parse(configuration["EmailVerification:MaxResendCount"] ?? "5");
        _verificationSecret = configuration["EmailVerification:SecretKey"]
            ?? configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Email verification secret is not configured");
    }

    /// <summary>
    /// Старый одношаговый endpoint регистрации отключён: регистрация выполняется через код подтверждения email.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        return BadRequest("Email verification is required. Use /api/auth/register/start and /api/auth/register/confirm.");
    }

    /// <summary>
    /// Первый шаг регистрации: проверяет данные, создаёт временную регистрацию и отправляет код на email.
    /// </summary>
    [HttpPost("register/start")]
    [ProducesResponseType(typeof(StartRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> StartRegistration([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationError = await ValidateRegistrationRequestAsync(request, cancellationToken);
        if (validationError != null)
            return BadRequest(validationError);

        var email = Email.Create(request.Email).Value.ToLowerInvariant();
        var activeByEmail = await _unitOfWork.EmailVerificationCodes.GetLatestActiveByEmailAsync(email, cancellationToken);

        if (activeByEmail != null &&
            activeByEmail.LastSentAt.AddSeconds(_verificationCodeResendCooldownSeconds) > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                $"Please wait {_verificationCodeResendCooldownSeconds} seconds before requesting a new code.");
        }

        if (await _unitOfWork.EmailVerificationCodes.HasActiveUsernameAsync(request.Username, email, cancellationToken))
            return BadRequest("Username is already reserved by another pending registration");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var code = GenerateVerificationCode();
        var codeHash = HashVerificationCode(email, code);
        var expiresAt = DateTime.UtcNow.AddMinutes(_verificationCodeExpirationMinutes);

        if (activeByEmail == null)
        {
            activeByEmail = new EmailVerificationCode(
                email,
                request.Username,
                passwordHash,
                codeHash,
                expiresAt,
                GetClientIpAddress(),
                GetUserAgent());

            await _unitOfWork.EmailVerificationCodes.AddAsync(activeByEmail, cancellationToken);
        }
        else
        {
            activeByEmail.ReplacePendingRegistration(
                request.Username,
                passwordHash,
                codeHash,
                expiresAt,
                GetUserAgent());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _emailSender.SendEmailVerificationCodeAsync(email, request.Username, code, expiresAt, cancellationToken);

        _logger.LogDebug("Registration verification code sent to {Email}", email);

        return Ok(new StartRegistrationResponse
        {
            Email = email,
            ExpiresInSeconds = _verificationCodeExpirationMinutes * 60,
            ResendCooldownSeconds = _verificationCodeResendCooldownSeconds
        });
    }

    /// <summary>
    /// Второй шаг регистрации: подтверждает код, создаёт пользователя и выдаёт access/refresh token.
    /// </summary>
    [HttpPost("register/confirm")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required");

        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != VerificationCodeLength || !request.Code.All(char.IsDigit))
            return BadRequest("Verification code must contain 6 digits");

        var email = Email.Create(request.Email).Value.ToLowerInvariant();
        var pending = await _unitOfWork.EmailVerificationCodes.GetLatestActiveByEmailAsync(email, cancellationToken);

        if (pending == null)
            return BadRequest("Verification code is invalid or expired");

        if (pending.AttemptsCount >= MaxVerificationAttempts)
            return BadRequest("Too many invalid attempts. Request a new verification code.");

        var codeHash = HashVerificationCode(email, request.Code);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(codeHash),
            Encoding.UTF8.GetBytes(pending.CodeHash)))
        {
            pending.RegisterFailedAttempt();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BadRequest("Invalid verification code");
        }

        if (await _unitOfWork.Users.IsEmailTakenAsync(email))
            return BadRequest("Email is already registered");

        if (await _unitOfWork.Users.IsUsernameTakenAsync(pending.Username))
            return BadRequest("Username is already taken");

        try
        {
            var user = new User(pending.Username, Email.Create(email), pending.PasswordHash);
            user.UpdateLastLogin();
            pending.Confirm();

            await _unitOfWork.Users.AddAsync(user);
            var authResponse = await CreateAuthResponseAsync(user, cancellationToken);

            _logger.LogDebug("User {Username} registered and confirmed successfully", user.Username);

            return Ok(authResponse);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Повторно отправляет код для активной незавершённой регистрации.
    /// </summary>
    [HttpPost("register/resend-code")]
    [ProducesResponseType(typeof(StartRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendRegistrationCode([FromBody] ResendRegistrationCodeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required");

        var email = Email.Create(request.Email).Value.ToLowerInvariant();
        var pending = await _unitOfWork.EmailVerificationCodes.GetLatestActiveByEmailAsync(email, cancellationToken);

        if (pending == null)
            return BadRequest("No active registration request found for this email");

        if (pending.ResendCount >= _maxVerificationCodeResends)
            return BadRequest("Maximum number of verification code resends reached");

        if (pending.LastSentAt.AddSeconds(_verificationCodeResendCooldownSeconds) > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                $"Please wait {_verificationCodeResendCooldownSeconds} seconds before requesting a new code.");
        }

        var code = GenerateVerificationCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(_verificationCodeExpirationMinutes);
        pending.RefreshCode(HashVerificationCode(email, code), expiresAt);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _emailSender.SendEmailVerificationCodeAsync(email, pending.Username, code, expiresAt, cancellationToken);

        _logger.LogDebug("Registration verification code resent to {Email}", email);

        return Ok(new StartRegistrationResponse
        {
            Email = email,
            ExpiresInSeconds = _verificationCodeExpirationMinutes * 60,
            ResendCooldownSeconds = _verificationCodeResendCooldownSeconds
        });
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

        var user = request.EmailOrUsername.Contains('@')
            ? await _unitOfWork.Users.GetByEmailAsync(request.EmailOrUsername)
            : await _unitOfWork.Users.GetByUsernameAsync(request.EmailOrUsername);

        if (user == null)
            return Unauthorized("Invalid credentials");

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

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        user.UpdateLastLogin();
        await _unitOfWork.SaveChangesAsync();

        var authResponse = await CreateAuthResponseAsync(user);

        _logger.LogDebug("User {Username} logged in successfully", user.Username);

        return Ok(authResponse);
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
    /// Обновить пару access/refresh token. Refresh token используется один раз и ротируется.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required");

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash);

        if (storedToken == null)
            return Unauthorized("Invalid refresh token");

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user == null)
            return Unauthorized("User not found");

        if (storedToken.IsRevoked)
        {
            await _unitOfWork.RefreshTokens.RevokeAllActiveByUserIdAsync(user.Id, GetClientIpAddress());
            await _unitOfWork.SaveChangesAsync();
            _logger.LogWarning("Refresh token reuse detected for user {UserId}; active refresh tokens revoked", user.Id);
            return Unauthorized("Refresh token was already used");
        }

        if (storedToken.IsExpired())
            return Unauthorized("Refresh token expired");

        if (user.IsBanActive())
            return Unauthorized("User is banned");

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

        storedToken.Revoke(GetClientIpAddress(), newRefreshTokenHash);

        var replacementToken = new RefreshToken(
            user.Id,
            newRefreshTokenHash,
            refreshExpiresAt,
            GetClientIpAddress(),
            GetUserAgent());

        await _unitOfWork.RefreshTokens.AddAsync(replacementToken);
        await _unitOfWork.SaveChangesAsync();

        var roles = user.Roles.Select(r => r.ToString()).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email.Value, roles);

        _logger.LogDebug("Tokens refreshed for user {UserId}", user.Id);

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email.Value,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshExpiresAt,
            Roles = roles
        });
    }

    /// <summary>
    /// Выход из системы: серверно отзывает текущий refresh token.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash);

            if (storedToken != null)
            {
                storedToken.Revoke(GetClientIpAddress());
                await _unitOfWork.SaveChangesAsync();
                _logger.LogDebug("Refresh token revoked for user {UserId}", storedToken.UserId);
            }
        }

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Выход со всех устройств: отзывает все активные refresh tokens текущего пользователя.
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _unitOfWork.RefreshTokens.RevokeAllActiveByUserIdAsync(userId, GetClientIpAddress());
        await _unitOfWork.SaveChangesAsync();

        _logger.LogDebug("All refresh tokens revoked for user {UserId}", userId);

        return Ok(new { message = "Logged out from all devices successfully" });
    }

    private async Task<string?> ValidateRegistrationRequestAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return "Username must be at least 3 characters";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required";

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return "Password must be at least 6 characters";

        if (request.Password != request.ConfirmPassword)
            return "Passwords do not match";

        try
        {
            _ = Email.Create(request.Email);
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }

        if (await _unitOfWork.Users.IsUsernameTakenAsync(request.Username))
            return "Username is already taken";

        if (await _unitOfWork.Users.IsEmailTakenAsync(request.Email))
            return "Email is already registered";

        return null;
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = user.Roles.Select(r => r.ToString()).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email.Value, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken(
            user.Id,
            refreshTokenHash,
            refreshTokenExpiresAt,
            GetClientIpAddress(),
            GetUserAgent()), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email.Value,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            Roles = roles
        };
    }

    private string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private string HashVerificationCode(string email, string code)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{_verificationSecret}:{normalizedEmail}:{code}"));
        return Convert.ToHexString(bytes);
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return Request.Headers.UserAgent.ToString();
    }
}
