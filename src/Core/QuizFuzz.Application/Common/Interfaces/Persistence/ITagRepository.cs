using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с тегами
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> IsNameTakenAsync(string name, CancellationToken cancellationToken = default);
}
