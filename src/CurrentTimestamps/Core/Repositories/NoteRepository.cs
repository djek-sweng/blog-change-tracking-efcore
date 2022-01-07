namespace Core.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly DatabaseContext _context;

    public NoteRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Notes.ToListAsync(cancellationToken);
    }

    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Notes.FirstOrDefaultAsync(note => note.Id == id, cancellationToken);
    }

    public async Task<Note?> InsertAsync(Note note, CancellationToken cancellationToken)
    {
        await _context.Notes.AddAsync(note, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(note.Id, cancellationToken);
    }

    public async Task DeleteAsync(Note note, CancellationToken cancellationToken)
    {
        _context.Notes.Remove(note);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
