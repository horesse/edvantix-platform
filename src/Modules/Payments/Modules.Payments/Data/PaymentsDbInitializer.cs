using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Payments.Data;

public sealed class PaymentsDbInitializer(
    PaymentsDbContext dbContext,
    ITenantSettingsService tenantSettingsService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ILogger<PaymentsDbInitializer> logger) : IDbInitializer
{
    /// <summary>Name of the single default tariff seeded for every new school — see <see cref="SeedAsync"/>.</summary>
    private const string DefaultTariffName = "Базовый тариф";

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Payments] applied migrations");
        }
    }

    /// <summary>
    /// Seeds one default <see cref="Tariff"/> so a brand-new school has something to attach to an
    /// invoice on day one. This reverses the module's original decision ("tariffs are created by
    /// the school through the API, not pre-populated") — the provisioning step for [[Multitenancy]]
    /// → "Шаги провижининга под новые модули" explicitly calls for a default tariff, mirroring what
    /// Identity's <c>IdentityDbInitializer</c> already does for school roles.
    ///
    /// <see cref="TariffKind.OneTime"/> was chosen over <c>PerLesson</c>/<c>PerMonth</c>: those two
    /// are accrual-based (<c>ITariffAccrualService</c> opposite <c>ISessionPlanQueryService</c>/
    /// <c>IAttendanceQueryService</c>) and only produce a sensible number once the school has
    /// courses, groups and sessions configured — meaningless as a day-one placeholder. `PerPackage`
    /// is excluded per the task brief (needs `LessonsCount`/`ValidDays` filled in thoughtfully, not
    /// a safe zero-effort default). `OneTime` needs neither — it is exactly "charge this amount
    /// once", the same shape as the manual invoice-line case the module's own docs call out
    /// ("учебник, экзамен, пробное"), so it degrades gracefully to "an editable placeholder line"
    /// with no assumption about the school's schedule. <c>CourseId: null</c> keeps this initializer
    /// independent of Curriculum's own seeding (see task brief — no cross-module dependency, no
    /// guaranteed run order between the two <c>IDbInitializer</c>s).
    ///
    /// Amount is deliberately <c>0m</c> — this codebase has no basis for guessing a school's real
    /// price, and a non-zero placeholder would look like a configured number instead of the "please
    /// edit me" it actually is. Currency comes from <see cref="ITenantSettingsService"/> (already
    /// defaulted to UTC/USD at tenant creation — see [[Multitenancy]] → "TenantSettings
    /// реализовано"), never hardcoded, so the seeded tariff always matches the school's own
    /// currency setting.
    ///
    /// Idempotent by <see cref="Tariff.Name"/> — matches Identity's <c>IdentityDbInitializer</c>
    /// "check before insert" style. Safe to call on every provisioning run / retry.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.Tariffs
            .AsNoTracking()
            .AnyAsync(t => t.Name == DefaultTariffName, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        var settings = await tenantSettingsService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);

        var tariff = Tariff.Create(
            name: DefaultTariffName,
            courseId: null,
            kind: TariffKind.OneTime,
            amount: 0m,
            currency: settings.Currency,
            lessonsCount: 0,
            validDays: 0,
            chargeOnExcusedAbsence: false);

        await dbContext.Tariffs.AddAsync(tariff, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Seeding default tariff '{TariffName}' ({Currency}) for '{TenantId}' Tenant.",
                DefaultTariffName,
                settings.Currency,
                multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id);
        }
    }
}
