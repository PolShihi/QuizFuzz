using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Application.Common.Interfaces.Services;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Subscriptions;

namespace QuizFuzz.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private const string PremiumPlanCode = "premium";
    private const string DemoSource = "Demo";
    private static readonly TimeSpan DemoMonthlyDuration = TimeSpan.FromDays(30);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<SubscriptionsController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(SubscriptionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var subscription = await _unitOfWork.UserSubscriptions.GetCurrentActiveByUserIdAsync(
            userId.Value,
            DateTime.UtcNow,
            cancellationToken);

        return Ok(ToDto(subscription));
    }

    [HttpPost("demo-monthly")]
    [ProducesResponseType(typeof(SubscriptionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ActivateDemoMonthly(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            return NotFound("User not found");

        var now = DateTime.UtcNow;
        var subscription = await _unitOfWork.UserSubscriptions.GetCurrentActiveByUserIdAsync(
            userId.Value,
            now,
            cancellationToken);

        if (subscription == null)
        {
            subscription = new UserSubscription(
                userId.Value,
                PremiumPlanCode,
                now,
                now.Add(DemoMonthlyDuration),
                DemoSource);

            await _unitOfWork.UserSubscriptions.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.ExtendTo(subscription.ExpiresAt.Add(DemoMonthlyDuration));
            _unitOfWork.UserSubscriptions.Update(subscription);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} activated demo monthly subscription until {ExpiresAt}",
            userId.Value,
            subscription.ExpiresAt);

        return Ok(ToDto(subscription));
    }

    private static SubscriptionStatusDto ToDto(UserSubscription? subscription)
    {
        if (subscription == null || !subscription.IsActive(DateTime.UtcNow))
        {
            return new SubscriptionStatusDto
            {
                IsActive = false,
                Status = UserSubscriptionStatus.Expired.ToString()
            };
        }

        return new SubscriptionStatusDto
        {
            IsActive = true,
            PlanCode = subscription.PlanCode,
            StartedAt = subscription.StartedAt,
            ExpiresAt = subscription.ExpiresAt,
            Status = subscription.Status.ToString(),
            Source = subscription.Source
        };
    }
}
