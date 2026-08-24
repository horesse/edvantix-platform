using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.StudyGroups.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Services;

public sealed class DraftInvoiceRefreshService(
    PaymentsDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService,
    ITariffAccrualService accrualService) : IDraftInvoiceRefreshService
{
    public async Task RefreshForGroupAsync(Guid studyGroupId, CancellationToken cancellationToken = default)
    {
        var drafts = await dbContext.StudentInvoices
            .Include(i => i.Lines)
            .Where(i => i.StudyGroupId == studyGroupId && i.Status == InvoiceStatus.Draft)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (drafts.Count == 0)
        {
            return;
        }

        foreach (var invoice in drafts)
        {
            await RefreshInvoiceAsync(invoice, studyGroupId, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RefreshInvoiceAsync(StudentInvoice invoice, Guid studyGroupId, CancellationToken cancellationToken)
    {
        if (invoice.Lines.Count != 1 || invoice.Lines[0].TariffId is not { } tariffId)
        {
            return;
        }

        var tariff = await dbContext.Tariffs
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tariffId, cancellationToken)
            .ConfigureAwait(false);
        if (tariff is null || tariff.Kind is not (TariffKind.PerLesson or TariffKind.PerMonth))
        {
            // Only the two accrual kinds that depend on Scheduling activity need refreshing —
            // OneTime/PerPackage lines are fixed at generation time by design.
            return;
        }

        var enrollments = await studyGroupQueryService
            .GetActiveEnrollmentsWithTariffAsync(studyGroupId, invoice.PeriodTo, cancellationToken)
            .ConfigureAwait(false);
        var enrollment = enrollments.FirstOrDefault(e => e.StudentId == invoice.StudentId);
        if (enrollment is null)
        {
            // No longer an active enrollment as of the invoice period — most likely unenrolled
            // mid-period. Rather than guess a prorated figure, clear the line: StudentInvoice.Issue
            // refuses an invoice with no lines, so this surfaces as "needs a human", not a silently
            // wrong amount.
            invoice.ReplaceLines([]);
            return;
        }

        var line = await accrualService
            .CalculateAsync(tariff, enrollment, studyGroupId, invoice.PeriodFrom, invoice.PeriodTo, cancellationToken)
            .ConfigureAwait(false);
        invoice.ReplaceLines(line is null ? [] : [(line.Description, tariff.Id, line.Quantity, line.UnitPrice)]);
    }
}
