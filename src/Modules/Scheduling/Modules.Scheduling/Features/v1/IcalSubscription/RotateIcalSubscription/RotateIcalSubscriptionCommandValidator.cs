using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.RotateIcalSubscription;

/// <summary>No fields to validate — the command carries no payload (the caller is the JWT subject).
/// Present to satisfy the handler/validator pairing rule (<c>Architecture.Tests</c>).</summary>
public sealed class RotateIcalSubscriptionCommandValidator : AbstractValidator<RotateIcalSubscriptionCommand>
{
    public RotateIcalSubscriptionCommandValidator()
    {
    }
}
