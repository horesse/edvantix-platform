using System.Globalization;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.People.Features.v1.Students.CreateStudent;
using Mediator;

namespace FSH.Modules.People.Features.v1.Students.ImportStudents;

public sealed class ImportStudentsCommandHandler(
    PeopleDbContext dbContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<ImportStudentsCommand, ImportStudentsResultDto>
{
    private const int ExpectedColumnCount = 8;
    private static readonly CreateStudentCommandValidator RowValidator = new();

    public async ValueTask<ImportStudentsResultDto> Handle(ImportStudentsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lines = command.CsvContent
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var results = new List<ImportStudentRowResultDto>();

        // Row 1 is the header (skipped); data rows are numbered from 2, matching how a
        // spreadsheet-editing manager would count them when fixing a rejected row.
        for (int i = 1; i < lines.Count; i++)
        {
            int rowNumber = i + 1;
            cancellationToken.ThrowIfCancellationRequested();

            var (parsed, error) = TryParseRow(lines[i]);
            if (parsed is null)
            {
                results.Add(new ImportStudentRowResultDto(rowNumber, false, null, error));
                continue;
            }

            var validation = await RowValidator.ValidateAsync(parsed, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                string message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                results.Add(new ImportStudentRowResultDto(rowNumber, false, null, message));
                continue;
            }

            if (command.DryRun)
            {
                results.Add(new ImportStudentRowResultDto(rowNumber, true, null, null));
                continue;
            }

            try
            {
                var student = Student.Create(
                    parsed.LastName, parsed.FirstName, parsed.MiddleName, parsed.BirthDate,
                    parsed.Phone, parsed.Email, parsed.ManagerUserId, parsed.Source);

                dbContext.Students.Add(student);

                var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
                await outboxStore.AddAsync(
                    new StudentCreatedIntegrationEvent(
                        Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                        Guid.NewGuid().ToString(), "People", student.Id, student.LastName, student.FirstName),
                    cancellationToken).ConfigureAwait(false);

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                results.Add(new ImportStudentRowResultDto(rowNumber, true, student.Id, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad row must not abort the rest of the file — see the "не всё-или-ничего"
                // requirement in the task backlog.
                results.Add(new ImportStudentRowResultDto(rowNumber, false, null, ex.Message));
            }
        }

        return new ImportStudentsResultDto(
            command.DryRun,
            results.Count,
            results.Count(r => r.Success),
            results.Count(r => !r.Success),
            results);
    }

    private static (CreateStudentCommand? Command, string? Error) TryParseRow(string line)
    {
        var fields = CsvLineParser.ParseLine(line);
        if (fields.Count != ExpectedColumnCount)
        {
            return (null, $"Expected {ExpectedColumnCount} columns, got {fields.Count}.");
        }

        string lastName = fields[0].Trim();
        string firstName = fields[1].Trim();
        string? middleName = fields[2].Trim() is { Length: > 0 } m ? m : null;
        string birthDateRaw = fields[3].Trim();
        string phone = fields[4].Trim();
        string email = fields[5].Trim();
        string managerUserId = fields[6].Trim();
        string? source = fields[7].Trim() is { Length: > 0 } s ? s : null;

        if (!DateOnly.TryParseExact(birthDateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var birthDate))
        {
            return (null, $"BirthDate '{birthDateRaw}' is not a valid yyyy-MM-dd date.");
        }

        return (new CreateStudentCommand(lastName, firstName, middleName, birthDate, phone, email, managerUserId, source), null);
    }
}
