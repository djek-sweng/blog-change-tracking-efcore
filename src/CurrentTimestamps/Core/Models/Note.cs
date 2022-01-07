namespace Core.Models;

public class Note : ICurrentTimestamps
{
    public Guid Id { get; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ChangedAt { get; set; }

    private Note(Guid id, string message)
    {
        Id = id;
        Message = message;
    }

    public static Note Create(string message)
    {
        return new Note(Guid.NewGuid(), message);
    }
}
