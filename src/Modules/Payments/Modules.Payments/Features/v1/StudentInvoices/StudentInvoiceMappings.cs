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

    public static PaymentConfirmationDto ToDto(this PaymentConfirmation p) => new(
        p.Id, p.InvoiceId, p.Amount, p.PaidOn, p.Method, p.Reference, p.ProofFileId, p.ConfirmedByUserId, p.ConfirmedAtUtc, p.ReversesId, p.Note);

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
        i.Lines.Select(l => l.ToDto()).ToList(),
        i.Payments.Select(p => p.ToDto()).ToList());
}
