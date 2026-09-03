using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Payments.Services;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkGenerateInvoices;

public sealed class BulkGenerateInvoicesCommandHandler(
    PaymentsDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService,
    ITariffAccrualService accrualService,
    IInvoiceNumberGenerator numberGenerator)
    : ICommandHandler<BulkGenerateInvoicesCommand, IReadOnlyList<Guid>>
{
    public async ValueTask<IReadOnlyList<Guid>> Handle(BulkGenerateInvoicesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await studyGroupQueryService.GetBriefAsync(command.StudyGroupId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        var enrollments = await studyGroupQueryService
            .GetActiveEnrollmentsWithTariffAsync(command.StudyGroupId, command.PeriodTo, cancellationToken)
            .ConfigureAwait(false);
        if (enrollments.Count == 0)
        {
            return [];
        }

        // Course-level fallback tariff — "GroupEnrollment.TariffId, иначе тариф курса" (see
        // docs/02 Модули/Payments.md → «Массовое выставление»). Ambiguity (several active tariffs
        // for the same course) resolves to the earliest created one; a school with that setup
        // should set an explicit TariffId per enrollment instead.
        var courseTariffs = await dbContext.Tariffs.AsNoTracking()
            .Where(t => t.CourseId == group.CourseId && t.IsActive)
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var fallbackTariff = courseTariffs.Count > 0 ? courseTariffs[0] : null;

        var explicitTariffIds = enrollments
            .Where(e => e.TariffId is not null)
            .Select(e => e.TariffId!.Value)
            .Distinct()
            .ToList();
        var explicitTariffs = explicitTariffIds.Count == 0
            ? new List<Tariff>()
            : await dbContext.Tariffs.AsNoTracking()
                .Where(t => explicitTariffIds.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var invoiceIds = new List<Guid>();

        // Resolve everything that needs a brand-new invoice first, so the whole batch draws a single
        // contiguous block of numbers from the per-tenant counter (one atomic reservation, no
        // per-student round-trip, no race with a parallel batch — see IInvoiceNumberGenerator).
        var pending = new List<(Guid StudentId, Tariff Tariff, AccrualLine Line)>();
        foreach (var enrollment in enrollments)
        {
            var existing = await dbContext.StudentInvoices
                .FirstOrDefaultAsync(
                    i => i.StudentId == enrollment.StudentId
                        && i.StudyGroupId == command.StudyGroupId
                        && i.PeriodFrom == command.PeriodFrom
                        && i.PeriodTo == command.PeriodTo,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                invoiceIds.Add(existing.Id);
                continue;
            }

            var tariff = (enrollment.TariffId is { } tariffId ? explicitTariffs.Find(t => t.Id == tariffId) : null) ?? fallbackTariff;
            if (tariff is null)
            {
                // No per-student override and no active course-level tariff — nothing to bill
                // against; the manager resolves this by setting a tariff and re-running (idempotent).
                continue;
            }

            var line = await accrualService
                .CalculateAsync(tariff, enrollment, command.StudyGroupId, command.PeriodFrom, command.PeriodTo, cancellationToken)
                .ConfigureAwait(false);
            if (line is null)
            {
                continue;
            }

            pending.Add((enrollment.StudentId, tariff, line));
        }

        if (pending.Count > 0)
        {
            var numbers = await numberGenerator.NextBatchAsync(pending.Count, cancellationToken).ConfigureAwait(false);
            for (var i = 0; i < pending.Count; i++)
            {
                var (studentId, tariff, line) = pending[i];
                var invoice = StudentInvoice.Create(
                    numbers[i], studentId, null, command.StudyGroupId, command.PeriodFrom, command.PeriodTo, command.DueDate, tariff.Currency, null);
                invoice.ReplaceLines([(line.Description, tariff.Id, line.Quantity, line.UnitPrice)]);
                if (command.IssueImmediately)
                {
                    invoice.Issue(command.PeriodTo);
                }

                dbContext.StudentInvoices.Add(invoice);
                invoiceIds.Add(invoice.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return invoiceIds;
    }
}
