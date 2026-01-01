namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Unit of Work для управления транзакциями
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories
    IUserRepository Users { get; }
    IQuestionRepository Questions { get; }
    IQuestionAnswerRepository QuestionAnswers { get; }
    IFuzzyAliasRepository FuzzyAliases { get; }
    ITagRepository Tags { get; }
    IRoomRepository Rooms { get; }
    IGameSessionRepository GameSessions { get; }
    IGameRoundRepository GameRounds { get; }
    IPlayerAnswerRepository PlayerAnswers { get; }
    IScoreboardRepository Scoreboards { get; }
    IInvitationRepository Invitations { get; }
    IModerationActionRepository ModerationActions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
