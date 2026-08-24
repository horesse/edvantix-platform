namespace FSH.Modules.Payments.Contracts.Dtos;

public sealed record DebtorDto(
    Guid StudentId,
    decimal Debt,
    int OverdueInvoiceCount,
    DateOnly OldestDueDate);
