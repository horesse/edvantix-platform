namespace FSH.Modules.Payments.Contracts.Dtos;

public sealed record InvoiceLineDto(
    Guid Id,
    string Description,
    Guid? TariffId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount);
