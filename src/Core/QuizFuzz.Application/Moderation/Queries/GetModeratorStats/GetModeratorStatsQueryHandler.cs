using MediatR;
using Microsoft.Extensions.Logging;
using QuizFuzz.Application.Common.Interfaces.Persistence;

namespace QuizFuzz.Application.Moderation.Queries.GetModeratorStats;

/// <summary>
/// Запрос статистики модератора
/// </summary>
public record GetModeratorStatsQuery : IRequest<ModeratorStatsDto>
{
    public Guid ModeratorId { get; init; }
}

/// <summary>
/// DTO статистики модератора
/// </summary>
public record ModeratorStatsDto
{
    public int TotalActions { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }
    public int RequestChangesCount { get; init; }
    public DateTime? LastActionDate { get; init; }
}

/// <summary>
/// Обработчик запроса статистики модератора
/// </summary>
public class GetModeratorStatsQueryHandler : IRequestHandler<GetModeratorStatsQuery, ModeratorStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetModeratorStatsQueryHandler> _logger;

    public GetModeratorStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetModeratorStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ModeratorStatsDto> Handle(
        GetModeratorStatsQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Fetching moderator stats. ModeratorId: {ModeratorId}",
            request.ModeratorId);

        try
        {
            var stats = await _unitOfWork.ModerationActions.GetModeratorStatsAsync(
                request.ModeratorId, 
                cancellationToken);

            _logger.LogDebug(
                "Stats retrieved. ModeratorId: {ModeratorId}, TotalActions: {TotalActions}, Approved: {Approved}, Rejected: {Rejected}",
                request.ModeratorId, stats.TotalActions, stats.ApprovedCount, stats.RejectedCount);

            return new ModeratorStatsDto
            {
                TotalActions = stats.TotalActions,
                ApprovedCount = stats.ApprovedCount,
                RejectedCount = stats.RejectedCount,
                RequestChangesCount = stats.RequestChangesCount,
                LastActionDate = stats.LastActionDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Error fetching moderator stats. ModeratorId: {ModeratorId}",
                request.ModeratorId);
            
            // Возвращаем пустую статистику при ошибке
            return new ModeratorStatsDto
            {
                TotalActions = 0,
                ApprovedCount = 0,
                RejectedCount = 0,
                RequestChangesCount = 0,
                LastActionDate = null
            };
        }
    }
}
