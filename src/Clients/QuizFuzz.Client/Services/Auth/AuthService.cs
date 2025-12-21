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
            {
                await _localStorage.SetItemAsync(TokenKey, authResponse.Token);
                await _localStorage.SetItemAsync(RefreshTokenKey, authResponse.RefreshToken);
                await _localStorage.SetItemAsync(UserKey, authResponse.User);
            }

            return authResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            
            if (authResponse != null)
            {
                await _localStorage.SetItemAsync(TokenKey, authResponse.Token);
                await _localStorage.SetItemAsync(RefreshTokenKey, authResponse.RefreshToken);
                await _localStorage.SetItemAsync(UserKey, authResponse.User);
            }

            return authResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        await _localStorage.RemoveItemAsync(UserKey);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
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
}
