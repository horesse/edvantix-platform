namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary>Write-side shape for <c>CreateStudentInvoiceCommand</c>/<c>UpdateStudentInvoiceCommand</c>
/// — the command replaces the whole line set in one call (see <c>StudentInvoice.ReplaceLines</c>).</summary>
public sealed record InvoiceLineInput(string Description, Guid? TariffId, decimal Quantity, decimal UnitPrice);
