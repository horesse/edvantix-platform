using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.RevokeIcalSubscription;

/// <summary>No payload to validate — present to satisfy the handler/validator pairing rule
/// (<c>Architecture.Tests</c>).</summary>
public sealed class RevokeIcalSubscriptionCommandValidator : AbstractValidator<RevokeIcalSubscriptionCommand>
{
    public RevokeIcalSubscriptionCommandValidator()
    {
    }
}
