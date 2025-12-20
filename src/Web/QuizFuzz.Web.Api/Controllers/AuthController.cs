using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Auth.Commands.Login;
using QuizFuzz.Application.Auth.Commands.Register;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Domain.ValueObjects;

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
    [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length < 3)
            return BadRequest("Username must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(command.Email))
            return BadRequest("Email is required");

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters");

        // Check if username or email already exists
        if (await _unitOfWork.Users.IsUsernameTakenAsync(command.Username))
            return BadRequest("Username is already taken");

        if (await _unitOfWork.Users.IsEmailTakenAsync(command.Email))
            return BadRequest("Email is already registered");

        try
        {
            // Create email value object
            var email = Email.Create(command.Email);

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(command.Password);

            // Create user
            var user = new User(command.Username, email, passwordHash);

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate tokens
            var roles = user.Roles.Select(r => r.ToString());
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, email.Value, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _logger.LogInformation("User {Username} registered successfully", command.Username);

            return Ok(new RegisterResult
            {
                UserId = user.Id,
                Username = user.Username,
                Email = email.Value,
                AccessToken = accessToken,
                RefreshToken = refreshToken
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
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EmailOrUsername))
            return BadRequest("Email or username is required");

        if (string.IsNullOrWhiteSpace(command.Password))
            return BadRequest("Password is required");

        // Try to find user by email or username
        User? user = null;

        if (command.EmailOrUsername.Contains('@'))
        {
            user = await _unitOfWork.Users.GetByEmailAsync(command.EmailOrUsername);
        }
        else
        {
            user = await _unitOfWork.Users.GetByUsernameAsync(command.EmailOrUsername);
        }

        if (user == null)
            return Unauthorized("Invalid credentials");

        // Check if user is banned
        if (user.IsBanned)
        {
            if (user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow)
                return Unauthorized($"Account is banned until {user.BannedUntil.Value:yyyy-MM-dd HH:mm}");
            
            if (!user.BannedUntil.HasValue)
                return Unauthorized("Account is permanently banned");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        // Update last login
        user.UpdateLastLogin();
        await _unitOfWork.SaveChangesAsync();

        // Generate tokens
        var roles = user.Roles.Select(r => r.ToString());
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email.Value, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        return Ok(new LoginResult
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email.Value,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Roles = roles
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
}
