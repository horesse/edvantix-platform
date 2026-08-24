using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Payments;

/// <summary>Right <c>SchoolAdmin</c>-level — see <c>PaymentsPermissions.StudentPayments.Revoke</c> and
/// docs/02 Модули/Payments.md → «StudentPayments.Confirm — самое чувствительное право».</summary>
public sealed record ReversePaymentCommand(Guid PaymentId, string? Note) : ICommand<Guid>;
