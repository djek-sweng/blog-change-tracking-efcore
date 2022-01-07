namespace Core.Database.Abstractions;

public interface ICurrentTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime? ChangedAt { get; set; }
}
