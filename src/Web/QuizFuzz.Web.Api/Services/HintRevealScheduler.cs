using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Shared.Dtos.Game;
using QuizFuzz.Web.Api.Hubs;

namespace QuizFuzz.Web.Api.Services;

public sealed class HintRevealScheduler : IHintRevealScheduler, IDisposable
{
    private const string RoomGroupPrefix = "room:";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<HintRevealScheduler> _logger;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _roundTokens = new();

    public HintRevealScheduler(
        IServiceScopeFactory scopeFactory,
        IHubContext<GameHub> hubContext,
        ILogger<HintRevealScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    public void ScheduleHints(Guid roomId, Guid roundId)
    {
        StopHints(roundId);
        StartSchedule(roomId, roundId);
    }

    public void EnsureHintsScheduled(Guid roomId, Guid roundId)
    {
        if (_roundTokens.ContainsKey(roundId))
            return;

        StartSchedule(roomId, roundId);
    }

    private void StartSchedule(Guid roomId, Guid roundId)
    {
        var cts = new CancellationTokenSource();
        if (!_roundTokens.TryAdd(roundId, cts))
        {
            cts.Dispose();
            return;
        }

        _ = Task.Run(() => RunScheduleAsync(roomId, roundId, cts.Token), cts.Token);
    }

    public void StopHints(Guid roundId)
    {
        if (_roundTokens.TryRemove(roundId, out var cts))
        {
            try
            {
                cts.Cancel();
            }
            finally
            {
                cts.Dispose();
            }
        }
    }

    private async Task RunScheduleAsync(Guid roomId, Guid roundId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var round = await unitOfWork.GameRounds.GetWithDetailsAsync(roundId, cancellationToken);

            if (round == null || round.Status != RoundStatus.Active || round.StartedAt == null)
                return;

            var hints = round.Question.Hints
                .OrderBy(h => h.RevealTimeSec)
                .ThenBy(h => h.OrderIndex)
                .ToList();

            foreach (var hint in hints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var revealAt = round.StartedAt.Value.AddSeconds(hint.RevealTimeSec);
                var delay = revealAt - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);

                await using var checkScope = _scopeFactory.CreateAsyncScope();
                var checkUnitOfWork = checkScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentRound = await checkUnitOfWork.GameRounds.GetWithDetailsAsync(roundId, cancellationToken);

                if (currentRound == null || currentRound.Status != RoundStatus.Active || currentRound.StartedAt == null)
                    return;

                var currentHint = currentRound.Question.Hints.FirstOrDefault(h => h.Id == hint.Id);
                if (currentHint == null)
                    continue;

                var revealedHint = CreateRevealedHint(currentHint, currentRound.StartedAt.Value);
                await _hubContext.Clients.Group(GetRoomGroupName(roomId))
                    .SendAsync("HintRevealed", revealedHint, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a round ends before all hints are revealed.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error while scheduling hints for round {RoundId}", roundId);
        }
        finally
        {
            if (_roundTokens.TryRemove(roundId, out var cts))
                cts.Dispose();
        }
    }

    private static RevealedHintDto CreateRevealedHint(Hint hint, DateTime roundStartedAt)
    {
        return new RevealedHintDto
        {
            Id = hint.Id,
            OrderIndex = hint.OrderIndex,
            Text = hint.HintText,
            MediaUrl = hint.MediaAsset?.Url,
            RevealTimeSeconds = hint.RevealTimeSec,
            RevealedAtSeconds = Math.Max(0, (int)Math.Floor((DateTime.UtcNow - roundStartedAt).TotalSeconds))
        };
    }

    private static string GetRoomGroupName(Guid roomId) => $"{RoomGroupPrefix}{roomId}";

    public void Dispose()
    {
        foreach (var roundId in _roundTokens.Keys)
            StopHints(roundId);
    }
}
