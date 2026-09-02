using Stopwatch = System.Diagnostics.Stopwatch;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetDebtorsReport;

public sealed class GetDebtorsReportQueryHandler(
    PaymentsDbContext dbContext,
    TimeProvider timeProvider,
    IAuditClient auditClient)
    : IQueryHandler<GetDebtorsReportQuery, IReadOnlyList<DebtorDto>>
{
    public async ValueTask<IReadOnlyList<DebtorDto>> Handle(GetDebtorsReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var startedAt = Stopwatch.GetTimestamp();
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var q = dbContext.StudentInvoices.AsNoTracking()
            .Where(StudentInvoiceQueries.OverdueBefore(today));
        if (query.StudyGroupId is { } studyGroupId)
        {
            q = q.Where(i => i.StudyGroupId == studyGroupId);
        }

        var overdue = await q.ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = overdue
            .GroupBy(i => i.StudentId)
            .Select(g => new DebtorDto(
                g.Key,
                g.Sum(i => i.Total - i.PaidAmount),
                g.Count(),
                g.Min(i => i.DueDate)))
            .OrderByDescending(d => d.Debt)
            .ToList();

        // Non-CRUD, financially sensitive read — no entity changes, so the EF interceptor sees
        // nothing. Record it explicitly (who pulled the debtors list, when, how wide).
        await auditClient.WriteActivityAsync(
            ActivityKind.Query,
            "GetDebtorsReport",
            statusCode: 200,
            durationMs: (int)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
            responsePreview: new
            {
                studyGroupId = query.StudyGroupId,
                debtors = result.Count,
                totalDebt = result.Sum(d => d.Debt),
            },
            source: "Payments",
            ct: cancellationToken).ConfigureAwait(false);

        return result;
    }
}
