using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices;

internal static class StudentInvoiceMappings
{
    public static StudentInvoiceDto ToDto(this StudentInvoice i, DateOnly today) => new(
        i.Id,
        i.Number,
        i.StudentId,
        i.PayerGuardianId,
        i.StudyGroupId,
        i.PeriodFrom,
        i.PeriodTo,
        i.Total,
        i.PaidAmount,
        i.Currency,
        i.Status,
        i.IssuedOn,
        i.DueDate,
        i.IsOverdue(today),
        i.Comment,
        i.CreatedAtUtc,
        i.UpdatedAtUtc);

    public static InvoiceLineDto ToDto(this InvoiceLine l) => new(l.Id, l.Description, l.TariffId, l.Quantity, l.UnitPrice, l.Amount);

    public static StudentInvoiceDetailDto ToDetailDto(this StudentInvoice i, DateOnly today) => new(
        i.Id,
        i.Number,
        i.StudentId,
        i.PayerGuardianId,
        i.StudyGroupId,
        i.PeriodFrom,
        i.PeriodTo,
        i.Total,
        i.PaidAmount,
        i.Currency,
        i.Status,
        i.IssuedOn,
        i.DueDate,
        i.IsOverdue(today),
        i.Comment,
        i.CreatedAtUtc,
        i.UpdatedAtUtc,
        i.Lines.Select(l => l.ToDto()).ToList());
}
