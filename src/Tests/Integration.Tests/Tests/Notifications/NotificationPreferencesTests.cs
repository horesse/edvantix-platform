using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Notifications;

/// <summary>
/// `/notifications/preferences` GET/PUT and the effect of a stored opt-out on the school-domain
/// subscribers (a recipient who turned a type off gets no row; others still do).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class NotificationPreferencesTests
{
    private const string BasePath = "/api/v1/notifications";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public NotificationPreferencesTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Get_Returns_Full_Catalog_With_Defaults_Then_Put_Overrides()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var initial = await (await client.GetAsync($"{BasePath}/preferences"))
            .DeserializeAsync<List<NotificationPreferenceDto>>();
        initial.ShouldContain(p => p.Type == NotificationTypes.SessionCancelled && p.Email);
        initial.ShouldContain(p => p.Type == NotificationTypes.SessionReminder && !p.Email);

        var put = await client.PutAsJsonAsync($"{BasePath}/preferences", new[]
        {
            new { type = NotificationTypes.SessionCancelled, inApp = true, email = false },
        });
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await (await client.GetAsync($"{BasePath}/preferences"))
            .DeserializeAsync<List<NotificationPreferenceDto>>();
        after.Single(p => p.Type == NotificationTypes.SessionCancelled).Email.ShouldBeFalse();
        after.Single(p => p.Type == NotificationTypes.SessionCancelled).InApp.ShouldBeTrue();
    }

    [Fact]
    public async Task Put_Rejects_Unknown_Type()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var put = await client.PutAsJsonAsync($"{BasePath}/preferences", new[]
        {
            new { type = "not.a.real.type", inApp = true, email = true },
        });

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Stored_OptOut_Suppresses_The_Row_For_That_Recipient_Only()
    {
        var studentUser = $"np-s-{Guid.NewGuid():N}"[..16];
        var guardianUser = $"np-g-{Guid.NewGuid():N}"[..16];
        Guid groupId = Guid.Empty;

        await InRootScope(async sp =>
        {
            var people = sp.GetRequiredService<PeopleDbContext>();
            var teacher = Teacher.Create("T", "T", null, "+1", "npt@t.test", null, null, null);
            people.Teachers.Add(teacher);

            var guardian = Guardian.Create("G", "G", "+2", $"{guardianUser}@t.test");
            guardian.LinkUser(guardianUser);
            people.Guardians.Add(guardian);

            var student = Student.Create("S", "S", null, new DateOnly(2012, 1, 1), "+3", "nps@t.test", "mgr", null);
            student.LinkUser(studentUser);
            student.AddGuardianLink(guardian.Id, "Parent", isPrimaryPayer: true);
            people.Students.Add(student);

            // Guardian opted out of "lesson cancelled" entirely.
            var db = sp.GetRequiredService<NotificationsDbContext>();
            db.NotificationPreferences.Add(NotificationPreference.Create(
                guardianUser, NotificationTypes.SessionCancelled, inAppEnabled: false, emailEnabled: false));

            var sg = sp.GetRequiredService<StudyGroupsDbContext>();
            var group = StudyGroup.Create(
                $"NP-{Guid.NewGuid():N}"[..12], "Pref Group", Guid.NewGuid(), teacher.Id,
                GroupFormat.Offline, 10, new DateOnly(2026, 1, 1), null, null, null, null);
            group.Enroll(student.Id, new DateOnly(2026, 1, 1), null, 0m);
            sg.StudyGroups.Add(group);

            await people.SaveChangesAsync();
            await db.SaveChangesAsync();
            await sg.SaveChangesAsync();
            groupId = group.Id;
        });

        await PublishAsync(new SessionCancelledIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "c", "Scheduling",
            Guid.NewGuid(), groupId, "snow day"));

        await InRootScope(async sp =>
        {
            var db = sp.GetRequiredService<NotificationsDbContext>();
            var studentRows = await db.Notifications
                .CountAsync(n => n.UserId == studentUser && n.Type == NotificationTypes.SessionCancelled);
            var guardianRows = await db.Notifications
                .CountAsync(n => n.UserId == guardianUser && n.Type == NotificationTypes.SessionCancelled);

            studentRows.ShouldBe(1, "the student kept the default (in-app on)");
            guardianRows.ShouldBe(0, "the guardian opted out");
        });
    }

    private async Task PublishAsync(IIntegrationEvent @event)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var tenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(TestConstants.RootTenantId);
        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        await sp.GetRequiredService<IEventBus>().PublishAsync(@event);
    }

    private async Task InRootScope(Func<IServiceProvider, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        await action(scope.ServiceProvider);
    }
}
