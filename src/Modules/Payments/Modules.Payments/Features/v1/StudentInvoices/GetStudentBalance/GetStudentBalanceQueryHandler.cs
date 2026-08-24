using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Payments.Features.v1.StudentInvoices;
using FSH.Modules.Scheduling.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentBalance;

public sealed class GetStudentBalanceQueryHandler(
    PaymentsDbContext dbContext,
    IAttendanceQueryService attendanceQueryService,
    TimeProvider timeProvider)
    : IQueryHandler<GetStudentBalanceQuery, StudentBalanceDto>
{
    public async ValueTask<StudentBalanceDto> Handle(GetStudentBalanceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Draft invoices don't count toward the balance — nothing has been billed yet. Cancelled
        // invoices never had money move (Cancel requires PaidAmount = 0), so they're moot either way.
        // Lines are needed here (unlike before) to find PerPackage invoices for the package balance.
        var invoices = await dbContext.StudentInvoices
            .Include(i => i.Lines)
            .AsNoTracking()
            .Where(i => i.StudentId == query.StudentId && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal charged = invoices.Sum(i => i.Total);
        decimal paid = invoices.Sum(i => i.PaidAmount);
        decimal debt = invoices.Sum(i => Math.Max(0, i.Total - i.PaidAmount));
        decimal advance = invoices.Sum(i => Math.Max(0, i.PaidAmount - i.Total));

        var overdue = invoices
            .Where(i => i.IsOverdue(today))
            .OrderBy(i => i.DueDate)
            .Select(i => i.ToDto(today))
            .ToList();

        var packages = await BuildPackageBalancesAsync(invoices, today, cancellationToken).ConfigureAwait(false);

        return new StudentBalanceDto(query.StudentId, charged, paid, debt, advance, overdue, packages);
    }

    /// <summary>Remaining-sessions projection for every <c>PerPackage</c> invoice — computed live from
    /// <c>IssuedOn</c> to today (or expiry, whichever is earlier) via
    /// <see cref="IAttendanceQueryService.CountHeldSessionsAsync"/>, never decremented by a stored
    /// counter (see docs/02 Модули/Payments.md → «Баланс»: "хранить агрегат опасно"). Every qualifying
    /// invoice gets its own entry — see <see cref="PackageBalanceDto"/> for why there's no single
    /// "active" package chosen among several.</summary>
    private async Task<IReadOnlyList<PackageBalanceDto>> BuildPackageBalancesAsync(
        List<StudentInvoice> invoices, DateOnly today, CancellationToken cancellationToken)
    {
        // A package invoice is recognized the same way DraftInvoiceRefreshService recognizes an
        // accrual-generated line: exactly one line, referencing a tariff, tied to a study group (the
        // group is required to look up attendance). Manually-edited multi-line invoices are excluded
        // by construction — same reasoning as the refresh service skipping them.
        var candidates = invoices
            .Where(i => i.StudyGroupId is not null && i.IssuedOn is not null && i.Lines.Count == 1 && i.Lines[0].TariffId is not null)
            .ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var tariffIds = candidates.Select(i => i.Lines[0].TariffId!.Value).Distinct().ToList();
        var packageTariffs = await dbContext.Tariffs
            .AsNoTracking()
            .Where(t => tariffIds.Contains(t.Id) && t.Kind == TariffKind.PerPackage)
            .ToDictionaryAsync(t => t.Id, cancellationToken)
            .ConfigureAwait(false);
        if (packageTariffs.Count == 0)
        {
            return [];
        }

        var packages = new List<PackageBalanceDto>();
        foreach (var invoice in candidates)
        {
            var tariffId = invoice.Lines[0].TariffId!.Value;
            if (!packageTariffs.TryGetValue(tariffId, out var tariff))
            {
                continue;
            }

            var issuedOn = invoice.IssuedOn!.Value;
            var studyGroupId = invoice.StudyGroupId!.Value;

            // ValidDays = 0 → no expiry (see docs/02 Модули/Payments.md → «Модель начисления»). Once
            // past expiry, the counting window freezes at the expiry date — sessions held afterward
            // are not attributed to this package (nor consumed from it), so RemainingCount stops
            // moving once IsExpired flips true. While still active there is no upper cap at "today" —
            // Held is a status, not a date check, and capping here would wrongly ignore sessions
            // Held slightly ahead of the query (clock skew, marking attendance early).
            DateOnly? expiresOn = tariff.ValidDays > 0 ? issuedOn.AddDays(tariff.ValidDays) : null;
            bool isExpired = expiresOn is { } expiry && expiry < today;
            var windowTo = isExpired ? expiresOn!.Value : DateOnly.MaxValue;

            int used = await attendanceQueryService
                .CountHeldSessionsAsync(invoice.StudentId, studyGroupId, issuedOn, windowTo, cancellationToken)
                .ConfigureAwait(false);
            int remaining = Math.Max(0, tariff.LessonsCount - used);

            packages.Add(new PackageBalanceDto(
                invoice.Id, invoice.Number, tariff.Id, tariff.Name, studyGroupId,
                tariff.LessonsCount, used, remaining, issuedOn, expiresOn, isExpired));
        }

        return packages;
    }
}
