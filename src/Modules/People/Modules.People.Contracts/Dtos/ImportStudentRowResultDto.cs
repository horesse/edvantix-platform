namespace FSH.Modules.People.Contracts.Dtos;

public sealed record ImportStudentRowResultDto(int RowNumber, bool Success, Guid? StudentId, string? Error);
