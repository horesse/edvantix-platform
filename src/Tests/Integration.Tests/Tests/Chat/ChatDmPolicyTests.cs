using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Chat;

/// <summary>
/// The direct-message policy (docs/02 Модули/Chat.md → «Ограничение личных сообщений») gating
/// <c>POST /chat/dms</c>, plus the <c>/chat/dm-settings</c> toggle. The allow-paths
/// (student ↔ own teacher, guardian ↔ ward's teacher, …) are unit-tested in
/// <c>Chat.Tests/DmPolicy/ChatDmPolicyTests</c>; here we cover default-deny, the staff bypass and
/// the settings endpoint.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ChatDmPolicyTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ChatDmPolicyTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task DmSettings_Default_Off_Then_Toggle_On()
    {
        using var admin = await _auth.CreateRootAdminClientAsync();

        var initial = await (await admin.GetAsync($"{ChatBasePath}/dm-settings"))
            .DeserializeAsync<ChatDmSettingsDto>();
        initial.AllowStudentToStudentDm.ShouldBeFalse();

        var put = await admin.PutAsJsonAsync($"{ChatBasePath}/dm-settings", new { allowStudentToStudentDm = true });
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await (await admin.GetAsync($"{ChatBasePath}/dm-settings"))
            .DeserializeAsync<ChatDmSettingsDto>();
        after.AllowStudentToStudentDm.ShouldBeTrue();

        // reset so other tests in the shared collection see the default
        await admin.PutAsJsonAsync($"{ChatBasePath}/dm-settings", new { allowStudentToStudentDm = false });
    }

    [Fact]
    public async Task NonStaff_Cannot_Dm_An_Unrelated_User()
    {
        using var admin = await _auth.CreateRootAdminClientAsync();
        var (aliceId, aliceEmail, alicePwd) = await RegisterAsync(admin, "dmp-alice");
        var (bobId, _, _) = await RegisterAsync(admin, "dmp-bob");

        using var alice = await _auth.CreateAuthenticatedClientAsync(aliceEmail, alicePwd);

        using var response = await alice.PostAsJsonAsync($"{ChatBasePath}/dms", new { userIds = new[] { bobId } });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "two unrelated non-staff users may not start a DM");
        _ = aliceId;
    }

    [Fact]
    public async Task Staff_Bypass_Lets_Admin_Dm_Anyone()
    {
        using var admin = await _auth.CreateRootAdminClientAsync();
        var (peerId, _, _) = await RegisterAsync(admin, "dmp-peer");

        using var response = await admin.PostAsJsonAsync($"{ChatBasePath}/dms", new { userIds = new[] { peerId } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<(string Id, string Email, string Password)> RegisterAsync(HttpClient admin, string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        const string password = "Test@1234!";

        var response = await admin.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = prefix,
            lastName = "Test",
            email,
            userName = $"{prefix}-{unique}",
            password,
            confirmPassword = password,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await response.DeserializeAsync<RegisterResult>()).UserId;

        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByIdAsync(id);
        if (user is not null && !user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        return (id, email, password);
    }

    private sealed record RegisterResult(string UserId);
}
