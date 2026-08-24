namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary>A read-side projection over invoices/payments — never a stored aggregate (see
/// docs/02 Модули/Payments.md → «Баланс»: "хранить агрегат опасно — рассинхронизируется").</summary>
/// <param name="StudentId">The student the balance is for.</param>
/// <param name="Charged">Sum of <c>Total</c> across non-Draft/non-Cancelled invoices.</param>
/// <param name="Paid">Sum of <c>PaidAmount</c> across the same invoices.</param>
/// <param name="Debt">Sum of each invoice's unpaid remainder (never negative per invoice).</param>
/// <param name="Advance">Sum of each invoice's overpayment (never negative per invoice).</param>
/// <param name="OverdueInvoices">Invoices past <c>DueDate</c> and not fully paid.</param>
/// <param name="Packages">One entry per non-cancelled <c>PerPackage</c> invoice — see
/// <see cref="PackageBalanceDto"/>.</param>
public sealed record StudentBalanceDto(
    Guid StudentId,
    decimal Charged,
    decimal Paid,
    decimal Debt,
    decimal Advance,
    IReadOnlyList<StudentInvoiceDto> OverdueInvoices,
    IReadOnlyList<PackageBalanceDto> Packages);
