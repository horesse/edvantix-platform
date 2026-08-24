namespace FSH.Modules.Payments.Contracts.Dtos;

public sealed record RevenueByMethodDto(PaymentMethod Method, decimal Amount);

public sealed record RevenueReportDto(
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal Total,
    IReadOnlyList<RevenueByMethodDto> ByMethod);
