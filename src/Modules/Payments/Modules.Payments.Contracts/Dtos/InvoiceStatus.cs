namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary>
/// Derived from <c>StudentInvoice.PaidAmount</c>/<c>Total</c> — never set directly by a command.
/// See docs/02 Модули/Payments.md → «Инварианты».
/// </summary>
public enum InvoiceStatus
{
    Draft,
    Issued,
    PartiallyPaid,
    Paid,
    Cancelled,
}
