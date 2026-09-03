using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.DuplicatePersonCandidates;

/// <summary>
/// Advisory duplicate lookup for the create-person dialogs. Strategy: narrow by an exact
/// case-insensitive last + first name match in SQL (<c>ILIKE</c> with no wildcards — small
/// result set), then confirm a contact match in memory. Phone is compared digits-only so
/// <c>"+7 900 111-22-33"</c> and <c>"+79001112233"</c> collapse to the same value. A candidate
/// needs the name match AND at least one contact match; name-only hits are dropped so families
/// sharing one phone across several children never trip the warning (the whole reason there is
/// no unique index — see docs/04 Задачи/EDX-018 Предупреждение о дубле человека.md).
/// </summary>
public sealed class FindDuplicatePersonCandidatesQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<FindDuplicatePersonCandidatesQuery, IReadOnlyList<DuplicatePersonCandidateDto>>
{
    private const int PerTypeCap = 25;

    public async ValueTask<IReadOnlyList<DuplicatePersonCandidateDto>> Handle(
        FindDuplicatePersonCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string lastName = (query.LastName ?? string.Empty).Trim();
        string firstName = (query.FirstName ?? string.Empty).Trim();
        string? phoneDigits = NormalizePhone(query.Phone);
        string? email = string.IsNullOrWhiteSpace(query.Email) ? null : query.Email.Trim();

        // Nothing to correlate on: no name, or no contact channel supplied.
        if ((lastName.Length == 0 && firstName.Length == 0) || (phoneDigits is null && email is null))
        {
            return [];
        }

        var students = await dbContext.Students.AsNoTracking()
            .Where(s => EF.Functions.ILike(s.LastName, lastName) && EF.Functions.ILike(s.FirstName, firstName))
            .Take(PerTypeCap)
            .Select(s => new PersonRow(s.Id, s.LastName, s.FirstName, s.MiddleName, s.Phone, s.Email))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var teachers = await dbContext.Teachers.AsNoTracking()
            .Where(t => EF.Functions.ILike(t.LastName, lastName) && EF.Functions.ILike(t.FirstName, firstName))
            .Take(PerTypeCap)
            .Select(t => new PersonRow(t.Id, t.LastName, t.FirstName, t.MiddleName, t.Phone, t.Email))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var guardians = await dbContext.Guardians.AsNoTracking()
            .Where(g => EF.Functions.ILike(g.LastName, lastName) && EF.Functions.ILike(g.FirstName, firstName))
            .Take(PerTypeCap)
            .Select(g => new PersonRow(g.Id, g.LastName, g.FirstName, null, g.Phone, g.Email))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = new List<DuplicatePersonCandidateDto>();
        Collect(results, "Student", students, phoneDigits, email);
        Collect(results, "Teacher", teachers, phoneDigits, email);
        Collect(results, "Guardian", guardians, phoneDigits, email);
        return results;
    }

    private static void Collect(
        List<DuplicatePersonCandidateDto> sink,
        string personType,
        IEnumerable<PersonRow> rows,
        string? phoneDigits,
        string? email)
    {
        foreach (var row in rows)
        {
            bool phoneMatch = phoneDigits is not null && NormalizePhone(row.Phone) == phoneDigits;
            bool emailMatch = email is not null
                && string.Equals(row.Email.Trim(), email, StringComparison.OrdinalIgnoreCase);

            if (phoneMatch || emailMatch)
            {
                sink.Add(new DuplicatePersonCandidateDto(
                    row.Id, personType, row.DisplayName, row.Phone, row.Email, phoneMatch, emailMatch));
            }
        }
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private sealed record PersonRow(
        Guid Id, string LastName, string FirstName, string? MiddleName, string Phone, string Email)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{LastName} {FirstName}"
            : $"{LastName} {FirstName} {MiddleName}";
    }
}
