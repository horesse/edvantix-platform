using FluentValidation;
using FSH.Modules.Tickets.Contracts.v1.Tickets;

namespace FSH.Modules.Tickets.Features.v1.Tickets.UpdateTicket;

public sealed class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(4096);
        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.RelatedStudentId!.Value).NotEqual(Guid.Empty).When(x => x.RelatedStudentId.HasValue);
        RuleFor(x => x.RelatedStudyGroupId!.Value).NotEqual(Guid.Empty).When(x => x.RelatedStudyGroupId.HasValue);
        RuleFor(x => x.RelatedInvoiceId!.Value).NotEqual(Guid.Empty).When(x => x.RelatedInvoiceId.HasValue);
    }
}
