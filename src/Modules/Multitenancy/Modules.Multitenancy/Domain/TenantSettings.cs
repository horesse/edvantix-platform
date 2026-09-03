using FSH.Framework.Core.Domain;

namespace FSH.Modules.Multitenancy.Domain;

/// <summary>
/// Per-tenant configuration that does not belong on the protected <c>AppTenantInfo</c>
/// (see <c>src/BuildingBlocks/Shared/Multitenancy/AppTenantInfo.cs</c>). Lives in
/// <see cref="Data.TenantDbContext"/> — the tenant catalog, not an isolated per-tenant schema —
/// by the same pattern as <see cref="TenantTheme"/>: an explicit <see cref="TenantId"/> column
/// with a unique index, since <c>TenantDbContext</c> applies no automatic tenant query filter.
/// </summary>
public class TenantSettings : BaseEntity<Guid>, IHasTenant, IAuditableEntity
{
    public const string DefaultTimeZoneId = "UTC";
    public const string DefaultCurrency = "USD";
    public const int DefaultDebtGraceDays = 7;

    /// <summary>Default invoice-number template — year-scoped counter, e.g. <c>2026-0001</c>
    /// (EDX-013; see docs/02 Модули/Payments.md → «Нумерация счетов»).</summary>
    public const string DefaultInvoiceNumberTemplate = "{YYYY}-{NNNN}";

    public string TenantId { get; private set; } = default!;

    /// <summary>IANA time zone identifier (e.g. "UTC", "Europe/Moscow").</summary>
    public string TimeZoneId { get; set; } = DefaultTimeZoneId;

    /// <summary>ISO 4217 currency code, stored upper-case (e.g. "USD").</summary>
    public string Currency { get; set; } = DefaultCurrency;

    /// <summary>
    /// When <c>true</c>, a student (and their guardian) loses access to lesson materials while any
    /// of the student's invoices is overdue by more than <see cref="DebtGraceDays"/> days —
    /// the schedule/attendance/invoices stay accessible. Default OFF. See EDX-015 and
    /// docs/02 Модули/Payments.md → «Автоблокировка доступа к материалам при задолженности».
    /// The rule itself lives in Payments (<c>IMaterialsAccessService</c>); this flag only arms it.
    /// </summary>
    public bool RestrictMaterialsOnDebt { get; set; }

    /// <summary>Grace period (days past an invoice's due date) before <see cref="RestrictMaterialsOnDebt"/>
    /// starts blocking. <c>0</c> = block the moment an invoice is overdue.</summary>
    public int DebtGraceDays { get; set; } = DefaultDebtGraceDays;

    /// <summary>
    /// Template for <c>StudentInvoice.Number</c> (EDX-013). Placeholders: <c>{YYYY}</c> / <c>{YY}</c>
    /// (year), <c>{MM}</c> (month), <c>{N…}</c> (zero-padded running counter). A year placeholder
    /// makes the counter reset per calendar year; otherwise it runs continuously. Owned as an opaque
    /// string here — Payments (<c>InvoiceNumberFormat</c>) renders it. Default
    /// <see cref="DefaultInvoiceNumberTemplate"/>.
    /// </summary>
    public string InvoiceNumberTemplate { get; set; } = DefaultInvoiceNumberTemplate;

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private TenantSettings() { } // EF Core

    /// <summary>
    /// Creates settings for a tenant. Falls back to <see cref="DefaultTimeZoneId"/> /
    /// <see cref="DefaultCurrency"/> when the caller doesn't supply a value — the default path
    /// used for the root tenant and for any tenant created without explicit values.
    /// </summary>
    public static TenantSettings Create(
        string tenantId,
        string? timeZoneId = null,
        string? currency = null,
        string? createdBy = null,
        bool restrictMaterialsOnDebt = false,
        int debtGraceDays = DefaultDebtGraceDays,
        string? invoiceNumberTemplate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId,
            Currency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.ToUpperInvariant(),
            RestrictMaterialsOnDebt = restrictMaterialsOnDebt,
            DebtGraceDays = debtGraceDays < 0 ? DefaultDebtGraceDays : debtGraceDays,
            InvoiceNumberTemplate = string.IsNullOrWhiteSpace(invoiceNumberTemplate)
                ? DefaultInvoiceNumberTemplate
                : invoiceNumberTemplate.Trim(),
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string timeZoneId,
        string currency,
        string? modifiedBy,
        bool restrictMaterialsOnDebt = false,
        int debtGraceDays = DefaultDebtGraceDays,
        string? invoiceNumberTemplate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        TimeZoneId = timeZoneId;
        Currency = currency.ToUpperInvariant();
        RestrictMaterialsOnDebt = restrictMaterialsOnDebt;
        DebtGraceDays = debtGraceDays < 0 ? 0 : debtGraceDays;

        // null/blank = "leave the existing template untouched" — lets callers that don't manage
        // numbering (e.g. the time-zone/currency form before EDX-014 ships) PUT settings without
        // clobbering a customised template.
        if (!string.IsNullOrWhiteSpace(invoiceNumberTemplate))
        {
            InvoiceNumberTemplate = invoiceNumberTemplate.Trim();
        }

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
