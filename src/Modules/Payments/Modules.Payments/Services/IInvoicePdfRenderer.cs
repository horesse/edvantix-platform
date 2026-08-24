using FSH.Modules.Payments.Contracts.Dtos;

namespace FSH.Modules.Payments.Services;

/// <summary>
/// Renders a <see cref="StudentInvoiceDetailDto"/> to PDF. Payments.md originally called for reusing
/// Billing's <c>IInvoicePdfRenderer</c> "without merging the modules" — but that interface lives in
/// <c>Modules.Billing.Services</c> (a runtime namespace, not <c>Modules.Billing.Contracts</c>), so
/// referencing it would cross the module boundary Architecture.Tests enforces. Payments declares its
/// own interface/implementation instead — same shape, independent of Billing, per QuestPDF.
/// </summary>
public interface IInvoicePdfRenderer
{
    byte[] Render(StudentInvoiceDetailDto invoice);
}
