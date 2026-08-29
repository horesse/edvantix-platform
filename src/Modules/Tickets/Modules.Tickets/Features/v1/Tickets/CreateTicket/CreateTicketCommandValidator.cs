using FluentValidation;
using FSH.Modules.Tickets.Contracts.v1.Tickets;

namespace FSH.Modules.Tickets.Features.v1.Tickets.CreateTicket;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(4096);

        RuleFor(x => x.RelatedStudentId!.Value).NotEqual(Guid.Empty).When(x => x.RelatedStudentId.HasValue);
        RuleFor(x => x.RelatedStudyGroupId!.Value).NotEqual(Guid.Empty).When(x => x.RelatedStudyGroupId.HasValue);
        RuleFor(x => x.RelatedInvoiceId!.Value).NotEqual(Guid.Empty).When(x => x.RelatedInvoiceId.HasValue);
    }
}
