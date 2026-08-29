using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Features.v1.Digest;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Integration.Tests.Tests.Notifications;

/// <summary>
/// The digest path: digestable e-mails (lesson cancelled/rescheduled, unexcused absence) are
/// buffered instead of sent one-by-one, and <see cref="NotificationDigestJob"/> flushes each
/// recipient's batch into one summary once the aggregation window has passed.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class NotificationDigestTests
{
    private readonly FshWebApplicationFactory _factory;

    public NotificationDigestTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Two_Cancellations_Are_Buffered_Then_Flushed_As_One_Summary()
    {
        var guardianUser = $"dg-g-{Guid.NewGuid():N}"[..16];
        var guardianEmail = $"{guardianUser}@dg.test";
        Guid groupId = Guid.Empty;

        var mail = (NoOpMailService)_factory.Services.GetRequiredService<FSH.Framework.Mailing.Services.IMailService>();
        mail.Clear();

        await InRootScope(async sp =>
        {
            var people = sp.GetRequiredService<PeopleDbContext>();
            var teacher = Teacher.Create("T", "T", null, "+1", "dgt@dg.test", null, null, null);
            people.Teachers.Add(teacher);
            var guardian = Guardian.Create("G", "G", "+2", guardianEmail);
            guardian.LinkUser(guardianUser);
            people.Guardians.Add(guardian);
            var student = Student.Create("S", "S", null, new DateOnly(2012, 1, 1), "+3", "dgs@dg.test", "mgr", null);
            student.AddGuardianLink(guardian.Id, "Parent", isPrimaryPayer: true);
            people.Students.Add(student);

            var sg = sp.GetRequiredService<StudyGroupsDbContext>();
            var group = StudyGroup.Create(
                $"DG-{Guid.NewGuid():N}"[..12], "Digest Group", Guid.NewGuid(), teacher.Id,
                GroupFormat.Offline, 10, new DateOnly(2026, 1, 1), null, null, null, null);
            group.Enroll(student.Id, new DateOnly(2026, 1, 1), null, 0m);
            sg.StudyGroups.Add(group);

            await people.SaveChangesAsync();
            await sg.SaveChangesAsync();
            groupId = group.Id;
        });

        for (var i = 0; i < 2; i++)
        {
            await PublishAsync(new SessionCancelledIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "c", "Scheduling",
                Guid.NewGuid(), groupId, $"reason {i}"));
        }

        // Buffered, not sent.
        await InRootScope(async sp =>
        {
            var db = sp.GetRequiredService<NotificationsDbContext>();
            var pending = await db.PendingNotificationDigests
                .Where(d => d.RecipientEmail == guardianEmail && d.SentAtUtc == null)
                .ToListAsync();
            pending.Count.ShouldBe(2);
        });
        mail.Sent.ShouldNotContain(m => m.To.Contains(guardianEmail));

        // Flush with a clock past the aggregation window.
        var job = new NotificationDigestJob(
            _factory.Services.GetRequiredService<IMultiTenantStore<AppTenantInfo>>(),
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new FixedTime(DateTimeOffset.UtcNow + NotificationDigestJob.AggregationWindow + TimeSpan.FromMinutes(1)),
            NullLogger<NotificationDigestJob>.Instance);
        await job.RunAsync(CancellationToken.None);

        mail.Sent.ShouldContain(m => m.To.Contains(guardianEmail) && m.Subject.Contains("2 update"));
        await InRootScope(async sp =>
        {
            var db = sp.GetRequiredService<NotificationsDbContext>();
            (await db.PendingNotificationDigests.CountAsync(d => d.RecipientEmail == guardianEmail && d.SentAtUtc == null))
                .ShouldBe(0);
        });
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
