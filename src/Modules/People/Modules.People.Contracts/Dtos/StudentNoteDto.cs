namespace FSH.Modules.People.Contracts.Dtos;

public sealed record StudentNoteDto(
    Guid Id,
    Guid StudentId,
    string Text,
    string AuthorUserId,
    DateTimeOffset CreatedAtUtc);
