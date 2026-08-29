using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Data;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.Events;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Chat;

/// <summary>
/// The study-group ⇆ chat-channel sync (docs/02 Модули/Chat.md → «Применение в Edvantix»):
/// a private channel is provisioned on <see cref="StudyGroupCreatedIntegrationEvent"/> and its id
/// linked back onto the group; membership follows enrolment (with the guardian-payer standing in
/// for a student who has no account); the channel is locked when the group finishes.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class StudyGroupChannelSyncTests
{
    private readonly FshWebApplicationFactory _factory;

    public StudyGroupChannelSyncTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Full_Lifecycle_Provisions_Syncs_And_Locks_The_Channel()
    {
        Guid groupId = Guid.Empty;
        Guid teacherId = Guid.Empty;
        Guid studentWithAccount = Guid.Empty;
        Guid studentNoAccount = Guid.Empty;
        const string teacherUser = "sgc-teacher";
        const string studentUser = "sgc-student";
        const string guardianUser = "sgc-guardian";

        await InRootScope(async sp =>
        {
            var people = sp.GetRequiredService<PeopleDbContext>();

            var teacher = Teacher.Create("Tutor", "Tara", null, "+1", "tara@sgc.test", null, null, null);
            teacher.LinkUser(teacherUser);
            people.Teachers.Add(teacher);

            var s1 = Student.Create("Alpha", "Ann", null, new DateOnly(2012, 1, 1), "+2", "ann@sgc.test", "mgr", null);
            s1.LinkUser(studentUser);

            var guardian = Guardian.Create("Beta", "Bob", "+3", "bob@sgc.test");
            guardian.LinkUser(guardianUser);
            people.Guardians.Add(guardian);

            var s2 = Student.Create("Beta", "Ben", null, new DateOnly(2013, 2, 2), "+4", "ben@sgc.test", "mgr", null);
            s2.AddGuardianLink(guardian.Id, "Parent", isPrimaryPayer: true);

            people.Students.AddRange(s1, s2);

            var sg = sp.GetRequiredService<StudyGroupsDbContext>();
            var group = StudyGroup.Create(
                $"SGC-{Guid.NewGuid():N}"[..12], "SGC Group", Guid.NewGuid(), teacher.Id,
                GroupFormat.Offline, 10, new DateOnly(2026, 1, 1), null, null, null, null);
            sg.StudyGroups.Add(group);

            await people.SaveChangesAsync();
            await sg.SaveChangesAsync();

            groupId = group.Id;
            teacherId = teacher.Id;
            studentWithAccount = s1.Id;
            studentNoAccount = s2.Id;
        });

        // 1. Created → channel provisioned, teacher seeded, id linked back to the group.
        await PublishAsync(new StudyGroupCreatedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, "SGC Group", Guid.NewGuid(), teacherId));

        await InRootScope(async sp =>
        {
            var chat = sp.GetRequiredService<ChatDbContext>();
            var channel = await chat.Channels.Include(c => c.Members)
                .SingleAsync(c => c.SourceStudyGroupId == groupId);
            channel.IsLocked.ShouldBeFalse();
            channel.Members.Select(m => m.UserId).ShouldContain(teacherUser);

            var sg = sp.GetRequiredService<StudyGroupsDbContext>();
            var group = await sg.StudyGroups.SingleAsync(g => g.Id == groupId);
            group.ChatChannelId.ShouldBe(channel.Id);
        });

        // 2a. Student with an account is added directly.
        await PublishAsync(new StudentEnrolledIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, studentWithAccount, new DateOnly(2026, 1, 2), null));

        // 2b. Student with no account → the primary-payer guardian stands in.
        await PublishAsync(new StudentEnrolledIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, studentNoAccount, new DateOnly(2026, 1, 2), null));

        await InRootScope(async sp =>
        {
            var chat = sp.GetRequiredService<ChatDbContext>();
            var members = (await chat.Channels.Include(c => c.Members)
                .SingleAsync(c => c.SourceStudyGroupId == groupId)).Members.Select(m => m.UserId).ToList();
            members.ShouldContain(studentUser);
            members.ShouldContain(guardianUser);
        });

        // 3. Unenrol the student with an account → removed (nobody else maps to that id).
        await PublishAsync(new StudentUnenrolledIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, studentWithAccount, new DateOnly(2026, 2, 1), null));

        await InRootScope(async sp =>
        {
            var chat = sp.GetRequiredService<ChatDbContext>();
            var members = (await chat.Channels.Include(c => c.Members)
                .SingleAsync(c => c.SourceStudyGroupId == groupId)).Members.Select(m => m.UserId).ToList();
            members.ShouldNotContain(studentUser);
            members.ShouldContain(guardianUser);
        });

        // 4. Finished → channel locked.
        await PublishAsync(new StudyGroupFinishedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, new DateOnly(2026, 3, 1)));

        await InRootScope(async sp =>
        {
            var chat = sp.GetRequiredService<ChatDbContext>();
            (await chat.Channels.SingleAsync(c => c.SourceStudyGroupId == groupId)).IsLocked.ShouldBeTrue();
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
