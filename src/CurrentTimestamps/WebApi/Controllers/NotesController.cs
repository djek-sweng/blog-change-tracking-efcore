namespace WebApi.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController : ControllerBase
{
    private readonly INoteRepository _noteRepository;

    public NotesController(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var notes = await _noteRepository.GetAllAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(id, cancellationToken);

        if (note is null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpPost]
    public async Task<IActionResult> InsertAsync([FromBody] NotePostDto noteDto, CancellationToken cancellationToken)
    {
        var note = Note.Create(noteDto.Message);

        note = await _noteRepository.InsertAsync(note, cancellationToken);

        if (note is null)
        {
            return BadRequest();
        }

        return CreatedAtRoute(note.Id, note);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] NotePutDto noteDto, CancellationToken cancellationToken)
    {
        var (id, message) = noteDto;

        var note = await _noteRepository.GetByIdAsync(id, cancellationToken);

        if (note is null)
        {
            return NotFound();
        }

        note.Message = message;

        await _noteRepository.SaveChangesAsync(cancellationToken);

        note = await _noteRepository.GetByIdAsync(id, cancellationToken);

        return Ok(note);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(id, cancellationToken);

        if (note is null)
        {
            return NotFound();
        }

        await _noteRepository.DeleteAsync(note, cancellationToken);

        return NoContent();
    }
}
