using FSH.Modules.Multitenancy.Domain;

namespace Multitenancy.Tests.Domain;

/// <summary>
/// Tests for the TenantSettings domain entity — time zone / currency configuration per tenant.
/// </summary>
public sealed class TenantSettingsTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_Should_SetTenantId()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void Create_Should_GenerateNewId()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_DefaultToUtcAndUsd_When_NoValuesProvided()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.TimeZoneId.ShouldBe("UTC");
        settings.Currency.ShouldBe("USD");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_DefaultTimeZone_When_ValueIsNullOrWhitespace(string? timeZoneId)
    {
        var settings = TenantSettings.Create("tenant-1", timeZoneId: timeZoneId);

        settings.TimeZoneId.ShouldBe(TenantSettings.DefaultTimeZoneId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_DefaultCurrency_When_ValueIsNullOrWhitespace(string? currency)
    {
        var settings = TenantSettings.Create("tenant-1", currency: currency);

        settings.Currency.ShouldBe(TenantSettings.DefaultCurrency);
    }

    [Fact]
    public void Create_Should_UseProvidedTimeZoneAndCurrency()
    {
        var settings = TenantSettings.Create("tenant-1", "Europe/Moscow", "RUB");

        settings.TimeZoneId.ShouldBe("Europe/Moscow");
        settings.Currency.ShouldBe("RUB");
    }

    [Fact]
    public void Create_Should_UppercaseCurrency()
    {
        var settings = TenantSettings.Create("tenant-1", currency: "usd");

        settings.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_Should_SetCreatedBy_When_Provided()
    {
        var settings = TenantSettings.Create("tenant-1", createdBy: "user-123");

        settings.CreatedBy.ShouldBe("user-123");
    }

    [Fact]
    public void Create_Should_AllowNullCreatedBy()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.CreatedBy.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_SetCreatedOnUtc()
    {
        var before = DateTimeOffset.UtcNow;

        var settings = TenantSettings.Create("tenant-1");
        var after = DateTimeOffset.UtcNow;

        settings.CreatedOnUtc.ShouldBeGreaterThanOrEqualTo(before);
        settings.CreatedOnUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_When_TenantIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => TenantSettings.Create(string.Empty));
    }

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        var settings1 = TenantSettings.Create("tenant-1");
        var settings2 = TenantSettings.Create("tenant-2");

        settings1.Id.ShouldNotBe(settings2.Id);
    }

    #endregion

    #region Update Method Tests

    [Fact]
    public void Update_Should_SetTimeZoneAndCurrency()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.Update("Europe/Moscow", "RUB", "modifier-user");

        settings.TimeZoneId.ShouldBe("Europe/Moscow");
        settings.Currency.ShouldBe("RUB");
    }

    [Fact]
    public void Update_Should_UppercaseCurrency()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.Update("UTC", "eur", null);

        settings.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Update_Should_SetLastModifiedOnUtc()
    {
        var settings = TenantSettings.Create("tenant-1");
        var before = DateTimeOffset.UtcNow;

        settings.Update("UTC", "USD", "modifier-user");
        var after = DateTimeOffset.UtcNow;

        settings.LastModifiedOnUtc.ShouldNotBeNull();
        settings.LastModifiedOnUtc!.Value.ShouldBeGreaterThanOrEqualTo(before);
        settings.LastModifiedOnUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Update_Should_SetLastModifiedBy()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.Update("UTC", "USD", "modifier-user");

        settings.LastModifiedBy.ShouldBe("modifier-user");
    }

    [Fact]
    public void Update_Should_AllowNullModifier()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.Update("UTC", "USD", null);

        settings.LastModifiedBy.ShouldBeNull();
        settings.LastModifiedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_ThrowArgumentException_When_TimeZoneIsEmpty()
    {
        var settings = TenantSettings.Create("tenant-1");

        Should.Throw<ArgumentException>(() => settings.Update(string.Empty, "USD", null));
    }

    [Fact]
    public void Update_Should_ThrowArgumentException_When_CurrencyIsEmpty()
    {
        var settings = TenantSettings.Create("tenant-1");

        Should.Throw<ArgumentException>(() => settings.Update("UTC", string.Empty, null));
    }

    [Fact]
    public void Update_Should_NotResetTenantId()
    {
        var settings = TenantSettings.Create("tenant-1");

        settings.Update("Europe/Moscow", "RUB", null);

        settings.TenantId.ShouldBe("tenant-1");
    }

    #endregion
}
