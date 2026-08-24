using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

/// <summary>Only a <c>Draft</c> invoice accepts this — see <c>StudentInvoice.EnsureDraft</c>.</summary>
public sealed record UpdateStudentInvoiceCommand(
    Guid InvoiceId,
    Guid? PayerGuardianId,
    Guid? StudyGroupId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    DateOnly DueDate,
    string? Comment,
    IReadOnlyList<InvoiceLineInput> Lines) : ICommand<Unit>;
