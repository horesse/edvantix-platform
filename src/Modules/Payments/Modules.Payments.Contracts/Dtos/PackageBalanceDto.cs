namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary>Remaining-sessions projection for one <c>PerPackage</c> invoice — like
/// <see cref="StudentBalanceDto"/> itself, computed live rather than stored (see docs/02 Модули/
/// Payments.md → «Баланс»/«Модель начисления»). Every non-<c>Draft</c>/non-<c>Cancelled</c> invoice
/// whose single line references a <c>PerPackage</c> tariff gets its own entry — there is no single
/// "active" package chosen among several; each is reported independently so the caller (or a school
/// that genuinely runs concurrent packages for the same student/group) sees the full picture.</summary>
/// <param name="InvoiceId">The package's invoice.</param>
/// <param name="InvoiceNumber">The invoice's human-readable number.</param>
/// <param name="TariffId">The <c>PerPackage</c> tariff the invoice was billed under.</param>
/// <param name="TariffName">Tariff name at query time (not frozen at issue).</param>
/// <param name="StudyGroupId">Group the package's sessions are counted against.</param>
/// <param name="LessonsCount">Package size — <c>Tariff.LessonsCount</c> at query time.</param>
/// <param name="UsedCount">Held sessions counted against the package so far.</param>
/// <param name="RemainingCount"><c>LessonsCount - UsedCount</c>, floored at 0.</param>
/// <param name="IssuedOn">Start of the counting window — the invoice's <c>IssuedOn</c>.</param>
/// <param name="ExpiresOn">Null when <c>Tariff.ValidDays</c> is 0 — the package never expires.</param>
/// <param name="IsExpired">True once <paramref name="ExpiresOn"/> has passed. <paramref
/// name="RemainingCount"/> stops changing after this — sessions held past expiry are not attributed
/// to the package.</param>
public sealed record PackageBalanceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid TariffId,
    string TariffName,
    Guid StudyGroupId,
    int LessonsCount,
    int UsedCount,
    int RemainingCount,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    bool IsExpired);
