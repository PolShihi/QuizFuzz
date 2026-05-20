using QuizFuzz.Client.Services.Auth;
using System.Net.Http.Headers;

namespace QuizFuzz.Client.Services.Api;

public class AuthenticatedHttpHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticatedHttpHandler(IAuthService authService, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _authService = authService;
    }
    
    // Конструктор для случаев когда innerHandler не передан (для тестов)
    public AuthenticatedHttpHandler(IAuthService authService) : base(new HttpClientHandler())
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Если получили 401, выходим из системы
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
        }

        return response;
    }
}
