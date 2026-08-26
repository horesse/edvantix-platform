using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Payments.Tests.Features;

namespace Payments.Tests.Data;

/// <summary>Covers the provisioning default seeded by <see cref="PaymentsDbInitializer.SeedAsync"/> —
/// see docs/04 Задачи/Задачи · Доработки каркаса.md → Multitenancy → "Шаги провижининга под новые
/// модули". Idempotency (no duplicate <see cref="Tariff"/> rows on re-run) mirrors
/// <c>IdentityDbInitializer</c>'s "check before insert" tests.</summary>
public sealed class PaymentsDbInitializerTests
{
    [Fact]
    public async Task SeedAsync_On_EmptyDatabase_Creates_Default_OneTime_Tariff_In_Tenant_Currency()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var initializer = CreateInitializer(db, currency: "EUR");

        await initializer.SeedAsync(CancellationToken.None);

        var tariffs = await db.Tariffs.ToListAsync();
        tariffs.Count.ShouldBe(1);
        var tariff = tariffs[0];
        tariff.Kind.ShouldBe(TariffKind.OneTime);
        tariff.CourseId.ShouldBeNull();
        tariff.Currency.ShouldBe("EUR");
        tariff.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task SeedAsync_Uses_Default_USD_When_TenantSettings_Fall_Back_To_Default()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var initializer = CreateInitializer(db, currency: "USD");

        await initializer.SeedAsync(CancellationToken.None);

        (await db.Tariffs.SingleAsync()).Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task SeedAsync_Called_Twice_Does_Not_Duplicate_The_Default_Tariff()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var initializer = CreateInitializer(db, currency: "USD");

        await initializer.SeedAsync(CancellationToken.None);
        await initializer.SeedAsync(CancellationToken.None);

        var tariffs = await db.Tariffs.ToListAsync();
        tariffs.Count.ShouldBe(1);
    }

    private static PaymentsDbInitializer CreateInitializer(PaymentsDbContext db, string currency)
    {
        var tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenantAccessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("tenant-acme", "tenant-acme")));

        var settingsService = Substitute.For<ITenantSettingsService>();
        settingsService.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantSettingsDto { TimeZoneId = "UTC", Currency = currency });

        return new PaymentsDbInitializer(db, settingsService, tenantAccessor, NullLogger<PaymentsDbInitializer>.Instance);
    }
}
