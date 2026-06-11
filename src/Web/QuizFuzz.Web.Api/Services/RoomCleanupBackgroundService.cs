using Microsoft.Extensions.Options;

namespace QuizFuzz.Web.Api.Services;

/// <summary>
/// Периодически запускает очистку пустых и устаревших комнат.
/// </summary>
public sealed class RoomCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RoomCleanupOptions _options;
    private readonly ILogger<RoomCleanupBackgroundService> _logger;

    public RoomCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<RoomCleanupOptions> options,
        ILogger<RoomCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Room cleanup background service is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.CheckIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IRoomCleanupService>();
                await cleanupService.CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Room cleanup failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
