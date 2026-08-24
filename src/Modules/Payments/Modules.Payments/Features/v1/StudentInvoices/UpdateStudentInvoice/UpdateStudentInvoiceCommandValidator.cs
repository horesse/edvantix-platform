using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.UpdateStudentInvoice;

public sealed class UpdateStudentInvoiceCommandValidator : AbstractValidator<UpdateStudentInvoiceCommand>
{
    public UpdateStudentInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.PeriodTo).GreaterThanOrEqualTo(x => x.PeriodFrom);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(512);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
