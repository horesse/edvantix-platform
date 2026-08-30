using System.Diagnostics.CodeAnalysis;
using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

/// <summary>
/// Thrown when an operation would push a tenant past its plan limit for a gauge-based
/// <see cref="QuotaResource"/> (active students, teachers, study groups, monthly sessions, …).
/// Maps to HTTP 402 Payment Required — the tenant keeps full access to existing data; only the
/// creation of a new billable entity is blocked until the plan is upgraded or usage drops.
/// </summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors",
    Justification = "Carries required structured context (resource/usage/limit); a message-only instance would be meaningless.")]
public sealed class QuotaExceededException : CustomException
{
    public QuotaResource Resource { get; }
    public long CurrentUsage { get; }
    public long Limit { get; }

    public QuotaExceededException(QuotaResource resource, long currentUsage, long limit)
        : base(
            $"{resource} plan limit reached ({currentUsage}/{limit}). Upgrade the plan to add more.",
            errors: null,
            HttpStatusCode.PaymentRequired)
    {
        Resource = resource;
        CurrentUsage = currentUsage;
        Limit = limit;
    }
}
