using QuizFuzz.Client.Services.Auth;
using System.Net;
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
        var retryRequest = await CloneHttpRequestMessageAsync(request, cancellationToken);
        await AttachAccessTokenAsync(request);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();

        var refreshed = await _authService.RefreshTokenAsync();
        if (!refreshed)
        {
            await _authService.LogoutAsync();
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = retryRequest
            };
        }

        await AttachAccessTokenAsync(retryRequest);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task AttachAccessTokenAsync(HttpRequestMessage request)
    {
        var token = await _authService.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
