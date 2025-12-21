using Microsoft.EntityFrameworkCore.Storage;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Infrastructure.Persistence.Repositories;

namespace QuizFuzz.Infrastructure.Persistence;

/// <summary>
/// Unit of Work для управления транзакциями и репозиториями
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    // Repositories
    private IUserRepository? _userRepository;
    private IQuestionRepository? _questionRepository;
    private IQuestionAnswerRepository? _questionAnswerRepository;
    private IFuzzyAliasRepository? _fuzzyAliasRepository;
    private ITagRepository? _tagRepository;
    private IRoomRepository? _roomRepository;
    private IGameSessionRepository? _gameSessionRepository;
    private IGameRoundRepository? _gameRoundRepository;
    private IPlayerAnswerRepository? _playerAnswerRepository;
    private IScoreboardRepository? _scoreboardRepository;
    private IInvitationRepository? _invitationRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _userRepository ??= new UserRepository(_context);

    public IQuestionRepository Questions =>
        _questionRepository ??= new QuestionRepository(_context);

    public IQuestionAnswerRepository QuestionAnswers =>
        _questionAnswerRepository ??= new QuestionAnswerRepository(_context);

    public IFuzzyAliasRepository FuzzyAliases =>
        _fuzzyAliasRepository ??= new FuzzyAliasRepository(_context);

    public ITagRepository Tags =>
        _tagRepository ??= new TagRepository(_context);

    public IRoomRepository Rooms =>
        _roomRepository ??= new RoomRepository(_context);

    public IGameSessionRepository GameSessions =>
        _gameSessionRepository ??= new GameSessionRepository(_context);

    public IGameRoundRepository GameRounds =>
        _gameRoundRepository ??= new GameRoundRepository(_context);

    public IPlayerAnswerRepository PlayerAnswers =>
        _playerAnswerRepository ??= new PlayerAnswerRepository(_context);

    public IScoreboardRepository Scoreboards =>
        _scoreboardRepository ??= new ScoreboardRepository(_context);

    public IInvitationRepository Invitations =>
        _invitationRepository ??= new InvitationRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
