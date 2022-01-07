namespace Core.Repositories;

public interface INoteRepository
{
    Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken);
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Note?> InsertAsync(Note note, CancellationToken cancellationToken);
    Task DeleteAsync(Note note, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}