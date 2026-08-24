using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

public sealed record CreateStudentInvoiceCommand(
    Guid StudentId,
    Guid? PayerGuardianId,
    Guid? StudyGroupId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    DateOnly DueDate,
    string Currency,
    string? Comment,
    IReadOnlyList<InvoiceLineInput> Lines) : ICommand<Guid>;
