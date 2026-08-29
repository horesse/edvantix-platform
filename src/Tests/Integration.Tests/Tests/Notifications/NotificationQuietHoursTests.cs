using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Features.v1.QuietHours;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Notifications;

/// <summary>
/// `/notifications/quiet-hours` GET/PUT and <c>INotificationQuietHoursService.IsQuietNowAsync</c>
/// (root tenant time zone is UTC, so "now" is easy to bracket).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class NotificationQuietHoursTests
{
    private const string BasePath = "/api/v1/notifications";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public NotificationQuietHoursTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Get_Returns_Disabled_By_Default_Then_Put_Persists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var initial = await (await client.GetAsync($"{BasePath}/quiet-hours"))
            .DeserializeAsync<NotificationQuietHoursDto>();
        initial.Enabled.ShouldBeFalse();

        var put = await client.PutAsJsonAsync($"{BasePath}/quiet-hours", new
        {
            enabled = true,
            startLocal = "21:00:00",
            endLocal = "08:00:00",
        });
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await (await client.GetAsync($"{BasePath}/quiet-hours"))
            .DeserializeAsync<NotificationQuietHoursDto>();
        after.Enabled.ShouldBeTrue();
        after.StartLocal.ShouldBe(new TimeOnly(21, 0));
    }

    [Fact]
    public async Task Put_Rejects_Zero_Width_Window_When_Enabled()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var put = await client.PutAsJsonAsync($"{BasePath}/quiet-hours", new
        {
            enabled = true,
            startLocal = "09:00:00",
            endLocal = "09:00:00",
        });

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IsQuietNow_Tracks_The_Window_Around_UtcNow()
    {
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var covering = (Start: now.AddHours(-1), End: now.AddHours(1));
        var excluding = (Start: now.AddHours(2), End: now.AddHours(3));

        await SetAndAssertAsync(covering.Start, covering.End, expectedQuiet: true);
        await SetAndAssertAsync(excluding.Start, excluding.End, expectedQuiet: false);
    }

    private async Task SetAndAssertAsync(TimeOnly start, TimeOnly end, bool expectedQuiet)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var service = scope.ServiceProvider.GetRequiredService<INotificationQuietHoursService>();
        await service.SetAsync(enabled: true, start, end);

        (await service.IsQuietNowAsync()).ShouldBe(expectedQuiet);
    }
}
