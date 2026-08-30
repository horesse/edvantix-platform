namespace FSH.Framework.Shared.Quota;

/// <summary>
/// Resources that can be metered per tenant. Counter-based resources (ApiCalls) are tracked
/// against a billing period; gauge-based resources reflect a point-in-time state and are resolved
/// on demand by registered gauge providers.
///
/// <para>
/// <see cref="ActiveStudents"/>, <see cref="ActiveTeachers"/>, <see cref="StudyGroups"/> and
/// <see cref="MonthlySessions"/> are domain gauges for the school product: a module owning the
/// data registers an <c>IQuotaGaugeProvider</c> that answers the live count from its own store.
/// They feed both the per-period <c>UsageSnapshot</c> rows and the soft "plan limit reached"
/// block on entity creation.
/// </para>
///
/// <para>
/// Values are persisted as <c>int</c> (UsageSnapshots, BillingPlan overage rates) — only ever
/// append new members, never reorder or insert.
/// </para>
/// </summary>
public enum QuotaResource
{
    ApiCalls,
    StorageBytes,
    Users,
    ActiveFeatureFlags,
    ActiveStudents,
    ActiveTeachers,
    StudyGroups,
    MonthlySessions
}
