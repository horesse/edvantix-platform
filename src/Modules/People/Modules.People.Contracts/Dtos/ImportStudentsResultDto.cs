namespace FSH.Modules.People.Contracts.Dtos;

public sealed record ImportStudentsResultDto(
    bool DryRun,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ImportStudentRowResultDto> Rows);
