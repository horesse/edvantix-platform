using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

/// <summary>
/// Bulk-imports students from CSV. Columns (with header row):
/// <c>LastName,FirstName,MiddleName,BirthDate,Phone,Email,ManagerUserId,Source</c> —
/// <c>MiddleName</c>/<c>Source</c> may be empty, <c>BirthDate</c> is <c>yyyy-MM-dd</c>.
/// <see cref="DryRun"/> (default true) validates every row and reports what would happen without
/// writing anything — the frontend calls it first for a preview, then re-sends with
/// <c>DryRun: false</c> to commit. Failing rows never block the rest of the file (see
/// docs/04 Задачи/Задачи · Новые модули.md — People, "Импорт из CSV").
/// </summary>
public sealed record ImportStudentsCommand(string CsvContent, bool DryRun = true) : ICommand<ImportStudentsResultDto>;
