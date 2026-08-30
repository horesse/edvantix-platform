using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Webhooks.Contracts.Authorization;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// Covers the "Сид ролей школы" work: <c>IdentityDbInitializer</c> seeds the five school roles
/// (<see cref="SchoolRoleConstants"/>) for a newly-provisioned tenant but not for root, and
/// <see cref="RolePermissionSyncer"/> tops an existing tenant up when the permission catalog
/// grows later. See docs/04 Задачи/Задачи · Доработки каркаса.md → Identity.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SchoolRolesSeedingTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public SchoolRolesSeedingTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task SeedAsync_Should_CreateAllFiveSchoolRoles_When_TenantIsProvisioned()
    {
        var tenant = await CreateSchoolTenantAsync();

        var roleNames = await GetRoleNamesAsync(tenant);

        foreach (var expected in SchoolRoleConstants.All)
        {
            roleNames.ShouldContain(expected, $"Tenant '{tenant.Id}' is missing school role '{expected}'.");
        }
    }

    [Fact]
    public async Task SeedAsync_Should_GiveSchoolAdmin_TheSameBundleAsAdmin()
    {
        var tenant = await CreateSchoolTenantAsync();

        var adminClaims = await GetClaimsAsync(tenant, RoleConstants.Admin);
        var schoolAdminClaims = await GetClaimsAsync(tenant, SchoolRoleConstants.SchoolAdmin);

        // SchoolAdmin = "all non-root permissions of the tenant", same bundle Admin gets — see
        // SchoolRolePermissions.ResolveSchoolAdmin.
        schoolAdminClaims.ShouldBe(adminClaims);
    }

    [Fact]
    public async Task SeedAsync_Should_GiveManager_UsersInvite_ButNotUsersCreateOrManageRoles()
    {
        var tenant = await CreateSchoolTenantAsync();

        var managerClaims = await GetClaimsAsync(tenant, SchoolRoleConstants.Manager);

        managerClaims.ShouldContain(IdentityPermissions.Users.Invite);
        managerClaims.ShouldContain(IdentityPermissions.Users.View);
        managerClaims.ShouldNotContain(IdentityPermissions.Users.Create);
        managerClaims.ShouldNotContain(IdentityPermissions.Users.ManageRoles);
    }

    [Fact]
    public async Task SeedAsync_Should_GiveSchool_ItsOwn_TenantScoped_WebhookClaims()
    {
        // "тенантные подписки школы работают" — the school gets the tenant-scoped Permissions.Webhooks.*
        // claims (not the root-only Platform.Webhooks). SchoolAdmin gets the full set; Manager, since
        // Webhooks is not an Identity-managed resource, gets it too.
        var tenant = await CreateSchoolTenantAsync();

        var schoolAdminClaims = await GetClaimsAsync(tenant, SchoolRoleConstants.SchoolAdmin);
        var managerClaims = await GetClaimsAsync(tenant, SchoolRoleConstants.Manager);

        foreach (var claims in new[] { schoolAdminClaims, managerClaims })
        {
            claims.ShouldContain(WebhooksPermissions.Subscriptions.View);
            claims.ShouldContain(WebhooksPermissions.Subscriptions.Create);
            claims.ShouldContain(WebhooksPermissions.Subscriptions.Delete);
            claims.ShouldContain(WebhooksPermissions.Subscriptions.Test);
        }
    }

    #endregion

    #region Root Tenant

    [Fact]
    public async Task SeedAsync_Should_NotCreateSchoolRoles_On_RootTenant()
    {
        var rootTenant = await GetTenantAsync(MultitenancyConstants.Root.Id);

        var roleNames = await GetRoleNamesAsync(rootTenant);

        foreach (var schoolRole in SchoolRoleConstants.All)
        {
            roleNames.ShouldNotContain(schoolRole, "The root tenant is the platform operator, not a school.");
        }
    }

    #endregion

    #region RolePermissionSyncer top-up

    [Fact]
    public async Task SyncAsync_Should_RestoreMissingManagerClaims_ForAlreadyProvisionedTenant()
    {
        var tenant = await CreateSchoolTenantAsync();

        // Simulate "Users.Invite was registered in a later release than this school's provisioning".
        await RemoveClaimAsync(tenant, SchoolRoleConstants.Manager, IdentityPermissions.Users.Invite);
        (await GetClaimsAsync(tenant, SchoolRoleConstants.Manager)).ShouldNotContain(IdentityPermissions.Users.Invite);

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var syncer = scope.ServiceProvider.GetRequiredService<RolePermissionSyncer>();
            await syncer.SyncAsync(CancellationToken.None);
        }

        (await GetClaimsAsync(tenant, SchoolRoleConstants.Manager)).ShouldContain(IdentityPermissions.Users.Invite);
    }

    [Fact]
    public async Task SyncAsync_Should_NotDuplicateSchoolRoleClaims_When_RunTwice()
    {
        var tenant = await CreateSchoolTenantAsync();
        var before = await GetClaimsAsync(tenant, SchoolRoleConstants.SchoolAdmin);

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var syncer = scope.ServiceProvider.GetRequiredService<RolePermissionSyncer>();
            await syncer.SyncAsync(CancellationToken.None);
            await syncer.SyncAsync(CancellationToken.None);
        }

        var after = await GetClaimsAsync(tenant, SchoolRoleConstants.SchoolAdmin);
        after.Count.ShouldBe(before.Count, "Syncer must not duplicate existing school-role permission claims.");
    }

    #endregion

    #region Helpers

    private async Task<AppTenantInfo> CreateSchoolTenantAsync()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"school-roles-{unique}";
        var adminEmail = $"admin-{unique}@school-roles.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"School Roles {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var body = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");

        await WaitForProvisioningAsync(rootClient, tenantId);

        var tenant = await GetTenantAsync(tenantId);

        // The provisioning status flips to "Completed" as soon as TenantProvisioningJob's last
        // step is marked done, which can be a beat ahead of the seeded rows being visible to a
        // freshly-opened scope/connection — the same race TenantSettingsTests works around by
        // retrying the first post-provisioning login. Wait for the Admin role specifically, since
        // every assertion in this file reads role/claim data through a brand-new scope.
        await WaitForRoleAsync(tenant, RoleConstants.Admin);

        return tenant;
    }

    private async Task<AppTenantInfo> GetTenantAsync(string tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(tenantId);
        tenant.ShouldNotBeNull($"Tenant '{tenantId}' must exist for this test.");
        return tenant;
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

    private async Task WaitForRoleAsync(AppTenantInfo tenant, string roleName, int maxRetries = 30)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            if ((await GetRoleNamesAsync(tenant)).Contains(roleName))
            {
                return;
            }
            await Task.Delay(500);
        }
        throw new TimeoutException($"Role '{roleName}' never became visible for tenant '{tenant.Id}'.");
    }

    private async Task<HashSet<string>> GetRoleNamesAsync(AppTenantInfo tenant)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var names = await roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return names.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<HashSet<string>> GetClaimsAsync(AppTenantInfo tenant, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.Roles.SingleAsync(r => r.Name == roleName);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var claims = await db.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync();

        return claims.ToHashSet(StringComparer.Ordinal);
    }

    private async Task RemoveClaimAsync(AppTenantInfo tenant, string roleName, string claimValue)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.Roles.SingleAsync(r => r.Name == roleName);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var toRemove = await db.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission && rc.ClaimValue == claimValue)
            .ToListAsync();

        db.RoleClaims.RemoveRange(toRemove);
        await db.SaveChangesAsync();
    }

    #endregion
}
