using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.Payments;

namespace FSH.Modules.Payments.Features.v1.Payments.ConfirmPayment;

public sealed class ConfirmPaymentCommandValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Reference).MaximumLength(128);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
