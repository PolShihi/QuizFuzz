using Blazored.LocalStorage;
using QuizFuzz.Shared.Dtos.Auth;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Auth;

public class AuthService : IAuthService
{
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string UserKey = "currentUser";

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse != null)
                await StoreAuthResponseAsync(authResponse);

            return authResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var startResult = await StartRegistrationAsync(request);
        return startResult == null ? null : null;
    }

    public async Task<StartRegistrationResponse?> StartRegistrationAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register/start", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<StartRegistrationResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponse?> ConfirmRegistrationAsync(ConfirmRegistrationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register/confirm", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse != null)
                await StoreAuthResponseAsync(authResponse);

            return authResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task<StartRegistrationResponse?> ResendRegistrationCodeAsync(ResendRegistrationCodeRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register/resend-code", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<StartRegistrationResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await GetRefreshTokenAsync();

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _httpClient.PostAsJsonAsync("api/auth/logout", new LogoutRequest
                {
                    RefreshToken = refreshToken
                });
            }
            catch
            {
                // Даже если сервер временно недоступен, локальную сессию нужно очистить.
            }
        }

        await ClearAuthStateAsync();
    }

    public async Task<bool> RefreshTokenAsync()
    {
        await _refreshLock.WaitAsync();

        try
        {
            var refreshToken = await GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await ClearAuthStateAsync();
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            });

            if (!response.IsSuccessStatusCode)
            {
                await ClearAuthStateAsync();
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse == null)
            {
                await ClearAuthStateAsync();
                return false;
            }

            await StoreAuthResponseAsync(authResponse);
            return true;
        }
        catch
        {
            await ClearAuthStateAsync();
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(RefreshTokenKey);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        return await _localStorage.GetItemAsync<UserDto>(UserKey);
    }

    private async Task StoreAuthResponseAsync(AuthResponse authResponse)
    {
        await _localStorage.SetItemAsync(TokenKey, authResponse.AccessToken);
        await _localStorage.SetItemAsync(RefreshTokenKey, authResponse.RefreshToken);
        await _localStorage.SetItemAsync(UserKey, new UserDto
        {
            Id = authResponse.UserId,
            Username = authResponse.Username,
            Email = authResponse.Email,
            Roles = authResponse.Roles.ToList()
        });
    }

    private async Task ClearAuthStateAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        await _localStorage.RemoveItemAsync(UserKey);
    }
}
