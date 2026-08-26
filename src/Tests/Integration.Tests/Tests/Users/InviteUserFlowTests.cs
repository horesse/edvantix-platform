using System.Text;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Tests.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Integration.Tests.Tests.Users;

/// <summary>
/// End-to-end "Приглашение по e-mail" flow (docs/04 Задачи/Задачи · Доработки каркаса.md →
/// Identity): send an invite, then accept it through the existing /reset-password endpoint.
/// Covers what the mocked-UserManager unit tests (InviteUserCommandHandlerTests,
/// UserRegistrationServiceTests) structurally cannot: real ASP.NET Identity role assignment,
/// the login gate on EmailConfirmed, and the token becoming single-use via the security-stamp
/// invalidation UserManager.ResetPasswordAsync does internally.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class InviteUserFlowTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public InviteUserFlowTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task InviteUser_Should_CreateUnconfirmedUser_WithRequestedRole()
    {
        // Arrange
        var tenantId = $"invite-{Guid.NewGuid():N}"[..20];
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        using var adminClient = await ProvisionTenantClientAsync(rootClient, tenantId);
        var email = $"teacher-{Guid.NewGuid():N}@invite-test.com";

        // Act
        var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Jamie",
            lastName = "Rivera",
            email,
            role = SchoolRoleConstants.Teacher,
        });

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, body);

        var (isConfirmed, isActive, roles) = await GetUserStateAsync(tenantId, email);
        isConfirmed.ShouldBeFalse("an invited account must stay unconfirmed until the invite is accepted.");
        isActive.ShouldBeTrue();
        roles.ShouldContain(SchoolRoleConstants.Teacher);

        // Cannot log in yet — the reset-password accept step is what confirms the e-mail.
        await Should.ThrowAsync<HttpRequestException>(
            () => _auth.GetTokenAsync(email, "whatever-the-random-password-was", tenantId));
    }

    #endregion

    #region Validation / Authorization

    [Fact]
    public async Task InviteUser_Should_Return400_When_RoleIsNotASeededSchoolRole()
    {
        var tenantId = $"invite-{Guid.NewGuid():N}"[..20];
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        using var adminClient = await ProvisionTenantClientAsync(rootClient, tenantId);

        var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Jamie",
            lastName = "Rivera",
            email = $"ghost-role-{Guid.NewGuid():N}@invite-test.com",
            role = "Ghost",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InviteUser_Should_Return403_When_CallerLacksInvitePermission()
    {
        // Arrange — a Basic-only user (no school role, no Users.Invite claim) tries to invite.
        var tenantId = $"invite-{Guid.NewGuid():N}"[..20];
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        using var adminClient = await ProvisionTenantClientAsync(rootClient, tenantId);
        var basicUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "invite-unauthorized", tenantId);
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicUser.Email, basicUser.Password, tenantId);

        // Act
        var response = await basicClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Jamie",
            lastName = "Rivera",
            email = $"forbidden-{Guid.NewGuid():N}@invite-test.com",
            role = SchoolRoleConstants.Teacher,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InviteUser_Should_Fail_And_NotCreateSecondAccount_When_EmailAlreadyInvited()
    {
        // Arrange
        var tenantId = $"invite-{Guid.NewGuid():N}"[..20];
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        using var adminClient = await ProvisionTenantClientAsync(rootClient, tenantId);
        var email = $"dup-{Guid.NewGuid():N}@invite-test.com";

        var first = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Jamie",
            lastName = "Rivera",
            email,
            role = SchoolRoleConstants.Teacher,
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — invite the same e-mail again.
        var second = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Someone",
            lastName = "Else",
            email,
            role = SchoolRoleConstants.Guardian,
        });

        // Assert
        ((int)second.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        var userCount = await CountUsersWithEmailAsync(tenantId, email);
        userCount.ShouldBe(1, "a duplicate invite must not create a second account.");
    }

    #endregion

    #region Accept Invite (via /reset-password)

    [Fact]
    public async Task AcceptInvite_Should_ConfirmEmail_AndAllowLogin_When_TokenIsValid()
    {
        // Arrange
        var tenantId = $"invite-{Guid.NewGuid():N}"[..20];
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        using var adminClient = await ProvisionTenantClientAsync(rootClient, tenantId);
        var email = $"accept-{Guid.NewGuid():N}@invite-test.com";
        const string newPassword = "AcceptedPa$$1!";

        var inviteResponse = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Jamie",
            lastName = "Rivera",
            email,
            role = SchoolRoleConstants.Guardian,
        });
        inviteResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var encodedToken = await GenerateResetTokenAsync(tenantId, email);

        using var anonClient = _factory.CreateClient();
        anonClient.DefaultRequestHeaders.Add("tenant", tenantId);

        // Act
        var acceptResponse = await anonClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/reset-password",
            new { email, password = newPassword, token = encodedToken });

        // Assert
        var acceptBody = await acceptResponse.Content.ReadAsStringAsync();
        acceptResponse.StatusCode.ShouldBe(HttpStatusCode.OK, acceptBody);

        var (isConfirmed, _, _) = await GetUserStateAsync(tenantId, email);
        isConfirmed.ShouldBeTrue("accepting the invite must confirm the e-mail (mailbox control was just proven).");

        var token = await _auth.GetTokenAsync(email, newPassword, tenantId);
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AcceptInvite_Should_RejectReusedToken()
    {
        // Arrange
        var tenantId = $"invite-{Guid.NewGuid():N}"[..20];
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        using var adminClient = await ProvisionTenantClientAsync(rootClient, tenantId);
        var email = $"onetime-{Guid.NewGuid():N}@invite-test.com";
        const string firstPassword = "FirstAccept$1!";
        const string secondPassword = "SecondAccept$1!";

        var inviteResponse = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/invite", new
        {
            firstName = "Jamie",
            lastName = "Rivera",
            email,
            role = SchoolRoleConstants.Student,
        });
        inviteResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var encodedToken = await GenerateResetTokenAsync(tenantId, email);

        using var anonClient = _factory.CreateClient();
        anonClient.DefaultRequestHeaders.Add("tenant", tenantId);

        var firstAccept = await anonClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/reset-password",
            new { email, password = firstPassword, token = encodedToken });
        firstAccept.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act — reuse the exact same token.
        var secondAccept = await anonClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/reset-password",
            new { email, password = secondPassword, token = encodedToken });

        // Assert — rejected, and the password from the first (successful) accept still works.
        ((int)secondAccept.StatusCode).ShouldBeGreaterThanOrEqualTo(400);

        var token = await _auth.GetTokenAsync(email, firstPassword, tenantId);
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();

        await Should.ThrowAsync<HttpRequestException>(
            () => _auth.GetTokenAsync(email, secondPassword, tenantId));
    }

    #endregion

    #region Helpers

    private async Task<HttpClient> ProvisionTenantClientAsync(HttpClient rootClient, string tenantId)
    {
        var adminEmail = $"{tenantId}-admin@invite-test.com";
        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Invite Test {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var body = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");

        await WaitForProvisioningAsync(rootClient, tenantId);
        return await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
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

    // Tenant context is set inline (Finbuckle AsyncLocal), same constraint as PasswordResetTests.
    private async Task<string> GenerateResetTokenAsync(string tenantId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        var raw = await userManager.GeneratePasswordResetTokenAsync(user);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
    }

    private async Task<(bool IsConfirmed, bool IsActive, IList<string> Roles)> GetUserStateAsync(string tenantId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        var roles = await userManager.GetRolesAsync(user);
        return (user.EmailConfirmed, user.IsActive, roles);
    }

    private async Task<int> CountUsersWithEmailAsync(string tenantId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        return await userManager.Users.CountAsync(u => u.Email == email);
    }

    #endregion
}
