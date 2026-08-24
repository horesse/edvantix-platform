namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary>A read-side projection over invoices/payments — never a stored aggregate (see
/// docs/02 Модули/Payments.md → «Баланс»: "хранить агрегат опасно — рассинхронизируется").</summary>
public sealed record StudentBalanceDto(
    Guid StudentId,
    decimal Charged,
    decimal Paid,
    decimal Debt,
    decimal Advance,
    IReadOnlyList<StudentInvoiceDto> OverdueInvoices);
