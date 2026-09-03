using FSH.Framework.Core.Domain;

namespace FSH.Modules.Payments.Domain;

/// <summary>
/// Per-tenant running counter behind <c>StudentInvoice.Number</c> (EDX-013). One row per
/// <see cref="Scope"/>: <c>"*"</c> for a continuous counter, or the 4-digit year (<c>"2026"</c>) when
/// the tenant's template is year-scoped (see <see cref="InvoiceNumberFormat.IsYearScoped"/>).
/// <para>
/// Never mutated through EF change-tracking — <c>IInvoiceNumberGenerator</c> reserves a block of
/// numbers with a single atomic <c>INSERT … ON CONFLICT DO UPDATE … RETURNING</c>, which serialises
/// concurrent batch issuance on the row lock. The mapped type exists only so the table travels with
/// the module's migrations; instances are materialised by EF, never constructed in code.
/// </para>
/// </summary>
public sealed class InvoiceNumberSequence : BaseEntity<Guid>
{
    /// <summary>Declared explicitly (rather than left as Finbuckle's shadow property) only so the
    /// <c>(TenantId, Scope)</c> unique index — the <c>ON CONFLICT</c> target — can be configured at
    /// model-build time. Finbuckle still owns writing it.</summary>
    public string TenantId { get; init; } = default!;

    /// <summary><c>"*"</c> (continuous) or a 4-digit calendar year (year-scoped template).</summary>
    public string Scope { get; init; } = default!;

    /// <summary>Highest number handed out so far for this scope. The next invoice gets <c>NextValue + 1</c>.</summary>
    public long NextValue { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }
}
