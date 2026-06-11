using QuizFuzz.Shared.Dtos.Subscriptions;

namespace QuizFuzz.Client.Services.Api;

public interface ISubscriptionsApiClient
{
    Task<SubscriptionStatusDto?> GetMySubscriptionAsync();
    Task<SubscriptionStatusDto?> ActivateDemoMonthlyAsync();
}
