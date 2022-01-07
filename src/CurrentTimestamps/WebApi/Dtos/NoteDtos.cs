namespace WebApi.Dtos;

public record NotePostDto(string Message);
public record NotePutDto(Guid Id, string Message);
