using System.Net.Http.Json;
using QuizFuzz.Shared.Dtos.Subscriptions;

namespace QuizFuzz.Client.Services.Api;

public class SubscriptionsApiClient : ISubscriptionsApiClient
{
    private readonly HttpClient _httpClient;

    public SubscriptionsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SubscriptionStatusDto?> GetMySubscriptionAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SubscriptionStatusDto>("api/subscriptions/me");
        }
        catch
        {
            return null;
        }
    }

    public async Task<SubscriptionStatusDto?> ActivateDemoMonthlyAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/subscriptions/demo-monthly", null);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SubscriptionStatusDto>();
        }
        catch
        {
            return null;
        }
    }
}
