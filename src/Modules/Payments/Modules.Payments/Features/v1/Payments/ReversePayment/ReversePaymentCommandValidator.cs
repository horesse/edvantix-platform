using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.Payments;

namespace FSH.Modules.Payments.Features.v1.Payments.ReversePayment;

public sealed class ReversePaymentCommandValidator : AbstractValidator<ReversePaymentCommand>
{
    public ReversePaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
