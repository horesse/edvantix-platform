#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// End-to-end coverage for the tenant settings feature (get / update, plus values supplied at
/// tenant creation) and cross-tenant isolation. Mirrors <see cref="TenantThemeTests"/> — settings
/// scoping is driven entirely by the resolved Finbuckle tenant context, and tenant B must never
/// see or mutate tenant A's settings.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantSettingsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private const string SettingsPath = $"{TestConstants.TenantsBasePath}/settings";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    private string _tenantA = default!;
    private string _tenantAAdminEmail = default!;
    private string _tenantB = default!;
    private string _tenantBAdminEmail = default!;

    public TenantSettingsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    public async Task InitializeAsync()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        _tenantA = $"settings-a-{unique}";
        _tenantB = $"settings-b-{unique}";
        _tenantAAdminEmail = $"admin-a-{unique}@settings.com";
        _tenantBAdminEmail = $"admin-b-{unique}@settings.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        await CreateTenantAsync(rootClient, _tenantA, _tenantAAdminEmail);
        await CreateTenantAsync(rootClient, _tenantB, _tenantBAdminEmail);
        await WaitForProvisioningAsync(rootClient, _tenantA);
        await WaitForProvisioningAsync(rootClient, _tenantB);

        _ = await GetTokenWithRetryAsync(_tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        _ = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Happy Path

    [Fact]
    public async Task GetSettings_Should_ReturnUtcAndUsd_When_CreatedWithoutExplicitValues()
    {
        // Arrange — tenant A/B were created without TimeZoneId/Currency in InitializeAsync.
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        // Act
        var response = await client.GetAsync(SettingsPath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.TimeZoneId.ShouldBe("UTC");
        settings.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task CreateTenant_Should_PersistExplicitTimeZoneAndCurrency()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"settings-explicit-{unique}";
        var adminEmail = $"admin-explicit-{unique}@settings.com";

        // Act — creation supplies both fields explicitly.
        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Explicit {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            timeZoneId = "Europe/Moscow",
            currency = "RUB",
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        await WaitForProvisioningAsync(rootClient, tenantId);

        // Assert — settings were created eagerly at tenant-creation time, not lazily on first read.
        using var client = await GetAuthenticatedClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);
        var response = await client.GetAsync(SettingsPath);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.TimeZoneId.ShouldBe("Europe/Moscow");
        settings.Currency.ShouldBe("RUB");
    }

    [Fact]
    public async Task UpdateSettings_Should_PersistAndReturnUpdatedValues_When_TenantAdminUpdatesOwnSettings()
    {
        // Arrange
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        // Act
        var updateResponse = await client.PutAsJsonAsync(SettingsPath, new { timeZoneId = "Asia/Tokyo", currency = "JPY" });

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync(SettingsPath);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var settings = await getResponse.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.TimeZoneId.ShouldBe("Asia/Tokyo");
        settings.Currency.ShouldBe("JPY");
    }

    [Fact]
    public async Task UpdateSettings_Should_UppercaseCurrency()
    {
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        var updateResponse = await client.PutAsJsonAsync(SettingsPath, new { timeZoneId = "UTC", currency = "eur" });
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync(SettingsPath);
        var settings = await getResponse.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.Currency.ShouldBe("EUR");
    }

    [Fact]
    public async Task GetSettings_Should_DefaultDebtRestriction_Off_With_SevenDayGrace()
    {
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        var response = await client.GetAsync(SettingsPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.RestrictMaterialsOnDebt.ShouldBeFalse();
        settings.DebtGraceDays.ShouldBe(7);
    }

    [Fact]
    public async Task UpdateSettings_Should_PersistDebtRestrictionFields()
    {
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);

        var updateResponse = await client.PutAsJsonAsync(SettingsPath, new
        {
            timeZoneId = "UTC",
            currency = "USD",
            restrictMaterialsOnDebt = true,
            debtGraceDays = 3,
        });
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync(SettingsPath);
        var settings = await getResponse.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.RestrictMaterialsOnDebt.ShouldBeTrue();
        settings.DebtGraceDays.ShouldBe(3);
    }

    #endregion

    #region Validation

    [Fact]
    public async Task UpdateSettings_Should_Return400_When_DebtGraceDaysOutOfRange()
    {
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        var response = await client.PutAsJsonAsync(SettingsPath, new
        {
            timeZoneId = "UTC",
            currency = "USD",
            restrictMaterialsOnDebt = true,
            debtGraceDays = 365,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSettings_Should_Return400_When_TimeZoneIsUnknown()
    {
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        var response = await client.PutAsJsonAsync(SettingsPath, new { timeZoneId = "Not/A_Zone", currency = "USD" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSettings_Should_Return400_When_CurrencyIsNotThreeLetters()
    {
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        var response = await client.PutAsJsonAsync(SettingsPath, new { timeZoneId = "UTC", currency = "US" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTenant_Should_Return400_When_TimeZoneIsUnknown()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = $"bad-tz-{unique}",
            name = $"Bad TZ {unique}",
            connectionString = (string?)null,
            adminEmail = $"bad-tz-{unique}@settings.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"bad-tz-{unique}.issuer",
            timeZoneId = "Not/A_Zone",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region AuthZ

    [Fact]
    public async Task GetSettings_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", _tenantA);

        var response = await client.GetAsync(SettingsPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSettings_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", _tenantA);

        var response = await client.PutAsJsonAsync(SettingsPath, new { timeZoneId = "UTC", currency = "USD" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Cross-Tenant Isolation

    [Fact]
    public async Task GetSettings_Should_StayInOwnTenant_When_TenantBAdminSendsTenantAHeader()
    {
        // Arrange — give tenant A distinctive settings (as A's own admin).
        using (var clientA = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA))
        {
            var update = await clientA.PutAsJsonAsync(SettingsPath, new { timeZoneId = "Asia/Tokyo", currency = "JPY" });
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Tenant B admin tries to read tenant A's settings by spoofing the header.
        var tokenB = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
        using var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new("Bearer", tokenB.AccessToken);
        clientB.DefaultRequestHeaders.Add("tenant", _tenantA); // spoof attempt — must be ignored

        // Act
        var response = await clientB.GetAsync(SettingsPath);

        // Assert — the override is gated to root, so B stays in B and sees its own (default) settings.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.TimeZoneId.ShouldNotBe("Asia/Tokyo");
        settings.TimeZoneId.ShouldBe("UTC");
        settings.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task UpdateSettings_Should_NotMutateTenantA_When_TenantBAdminSendsTenantAHeader()
    {
        // Arrange — tenant A's admin sets a known baseline.
        using (var clientA = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA))
        {
            var seed = await clientA.PutAsJsonAsync(SettingsPath, new { timeZoneId = "Europe/Moscow", currency = "RUB" });
            seed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Tenant B admin tries to overwrite tenant A's settings by spoofing the header.
        var tokenB = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
        using (var clientB = _factory.CreateClient())
        {
            clientB.DefaultRequestHeaders.Authorization = new("Bearer", tokenB.AccessToken);
            clientB.DefaultRequestHeaders.Add("tenant", _tenantA); // spoof attempt
            var attack = await clientB.PutAsJsonAsync(SettingsPath, new { timeZoneId = "UTC", currency = "USD" });
            // The write is accepted but applies to tenant B (where B is pinned), not A.
            attack.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Act — re-read tenant A's settings as A's own admin.
        using var verifyA = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var response = await verifyA.GetAsync(SettingsPath);

        // Assert — tenant A's settings are unchanged by B's spoofed write.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSettingsDto>(Json);
        settings.ShouldNotBeNull();
        settings.TimeZoneId.ShouldBe("Europe/Moscow");
        settings.Currency.ShouldBe("RUB");
    }

    #endregion

    #region Helpers

    private async Task<TokenResult> GetTokenWithRetryAsync(string email, string password, string tenant, int maxRetries = 30)
    {
        Exception? last = null;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.GetTokenAsync(email, password, tenant);
            }
            catch (HttpRequestException ex)
            {
                last = ex;
                await Task.Delay(500);
            }
        }
        throw last ?? new InvalidOperationException("token issuance failed");
    }

    private async Task<HttpClient> GetAuthenticatedClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Settings {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }

    // Local copy of the settings response shape — only the fields these tests assert on.
    private sealed record TenantSettingsDto
    {
        public string TimeZoneId { get; init; } = string.Empty;
        public string Currency { get; init; } = string.Empty;
        public bool RestrictMaterialsOnDebt { get; init; }
        public int DebtGraceDays { get; init; }
    }

    #endregion
}
