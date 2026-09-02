using System.Linq.Expressions;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices;

/// <summary>Shared EF-translatable predicates over <see cref="StudentInvoice"/> so the debtors
/// report and the EDX-015 materials-access check agree on what "overdue" means.</summary>
internal static class StudentInvoiceQueries
{
    /// <summary>Issued or partially-paid invoice whose due date is strictly before
    /// <paramref name="cutoff"/>. Pass <c>today</c> for "overdue" (debtors report) or
    /// <c>today.AddDays(-graceDays)</c> for "overdue past the grace window" (EDX-015).</summary>
    public static Expression<Func<StudentInvoice, bool>> OverdueBefore(DateOnly cutoff) =>
        i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
             && i.DueDate < cutoff;
}
