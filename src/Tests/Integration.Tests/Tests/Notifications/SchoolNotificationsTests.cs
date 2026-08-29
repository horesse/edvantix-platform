using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.Payments.Contracts.Events;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.Events;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Notifications;

/// <summary>
/// The school-domain notification subscribers (docs/02 Модули/Notifications.md → «Каталог
/// уведомлений»): a Scheduling/Payments/StudyGroups integration event lands inbox rows for the
/// right people (student, guardians, teacher / the payer).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SchoolNotificationsTests
{
    private readonly FshWebApplicationFactory _factory;

    public SchoolNotificationsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invoice_Issued_Notifies_The_Primary_Payer()
    {
        Guid studentId = Guid.Empty;
        var payerUser = $"pay-{Guid.NewGuid():N}"[..16];

        await InRootScope(async sp =>
        {
            var people = sp.GetRequiredService<PeopleDbContext>();
            var guardian = Guardian.Create("Pay", "Pam", "+1", $"{payerUser}@t.test");
            guardian.LinkUser(payerUser);
            people.Guardians.Add(guardian);
            var s = Student.Create("Inv", "Ivy", null, new DateOnly(2012, 1, 1), "+2", "ivy@t.test", "mgr", null);
            s.AddGuardianLink(guardian.Id, "Parent", isPrimaryPayer: true);
            people.Students.Add(s);
            await people.SaveChangesAsync();
            studentId = s.Id;
        });

        await PublishAsync(new StudentInvoiceIssuedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "c", "Payments",
            Guid.NewGuid(), studentId, null, 120.00m, new DateOnly(2026, 2, 1), "INV-777", "USD"));

        await InRootScope(async sp =>
        {
            var db = sp.GetRequiredService<NotificationsDbContext>();
            var rows = await db.Notifications
                .Where(n => n.UserId == payerUser && n.Type == NotificationTypes.InvoiceIssued)
                .ToListAsync();
            rows.ShouldNotBeEmpty();
            rows.ShouldContain(n => n.Title.Contains("INV-777"));
        });
    }

    [Fact]
    public async Task Absent_Attendance_Notifies_Guardians_But_Present_Does_Not()
    {
        Guid studentId = Guid.Empty;
        var guardianUser = $"att-{Guid.NewGuid():N}"[..16];

        await InRootScope(async sp =>
        {
            var people = sp.GetRequiredService<PeopleDbContext>();
            var guardian = Guardian.Create("Att", "Amy", "+1", $"{guardianUser}@t.test");
            guardian.LinkUser(guardianUser);
            people.Guardians.Add(guardian);
            var s = Student.Create("Abs", "Abe", null, new DateOnly(2012, 1, 1), "+2", "abe@t.test", "mgr", null);
            s.AddGuardianLink(guardian.Id, "Parent", isPrimaryPayer: true);
            people.Students.Add(s);
            await people.SaveChangesAsync();
            studentId = s.Id;
        });

        await PublishAsync(new AttendanceMarkedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "c", "Scheduling",
            Guid.NewGuid(), studentId, AttendanceStatus.Present));
        await PublishAsync(new AttendanceMarkedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "c", "Scheduling",
            Guid.NewGuid(), studentId, AttendanceStatus.Absent));

        await InRootScope(async sp =>
        {
            var db = sp.GetRequiredService<NotificationsDbContext>();
            var rows = await db.Notifications
                .Where(n => n.UserId == guardianUser && n.Type == NotificationTypes.AttendanceUnexcused)
                .ToListAsync();
            rows.Count.ShouldBe(1, "only the Absent mark should notify");
        });
    }

    [Fact]
    public async Task Session_Cancelled_Notifies_Enrolled_Students_Guardians_And_Teacher()
    {
        Guid groupId = Guid.Empty;
        var studentUser = $"sc-s-{Guid.NewGuid():N}"[..16];
        var guardianUser = $"sc-g-{Guid.NewGuid():N}"[..16];
        var teacherUser = $"sc-t-{Guid.NewGuid():N}"[..16];

        await InRootScope(async sp =>
        {
            var people = sp.GetRequiredService<PeopleDbContext>();
            var teacher = Teacher.Create("Tea", "Tom", null, "+9", $"{teacherUser}@t.test", null, null, null);
            teacher.LinkUser(teacherUser);
            people.Teachers.Add(teacher);

            var guardian = Guardian.Create("Gua", "Gus", "+3", $"{guardianUser}@t.test");
            guardian.LinkUser(guardianUser);
            people.Guardians.Add(guardian);

            var student = Student.Create("Stu", "Sue", null, new DateOnly(2012, 1, 1), "+2", "sue@t.test", "mgr", null);
            student.LinkUser(studentUser);
            student.AddGuardianLink(guardian.Id, "Parent", isPrimaryPayer: true);
            people.Students.Add(student);
            await people.SaveChangesAsync();

            var sg = sp.GetRequiredService<StudyGroupsDbContext>();
            var group = StudyGroup.Create(
                $"SC-{Guid.NewGuid():N}"[..12], "Cancel Group", Guid.NewGuid(), teacher.Id,
                GroupFormat.Offline, 10, new DateOnly(2026, 1, 1), null, null, null, null);
            group.Enroll(student.Id, new DateOnly(2026, 1, 1), null, 0m);
            sg.StudyGroups.Add(group);
            await sg.SaveChangesAsync();
            groupId = group.Id;
        });

        await PublishAsync(new SessionCancelledIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "c", "Scheduling",
            Guid.NewGuid(), groupId, "Teacher ill"));

        await InRootScope(async sp =>
        {
            var db = sp.GetRequiredService<NotificationsDbContext>();
            var recipients = await db.Notifications
                .Where(n => n.Type == NotificationTypes.SessionCancelled
                    && (n.UserId == studentUser || n.UserId == guardianUser || n.UserId == teacherUser))
                .Select(n => n.UserId)
                .Distinct()
                .ToListAsync();
            recipients.ShouldContain(studentUser);
            recipients.ShouldContain(guardianUser);
            recipients.ShouldContain(teacherUser);
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
