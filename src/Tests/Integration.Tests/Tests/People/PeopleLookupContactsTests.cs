using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.People;

/// <summary>
/// <see cref="IPeopleLookupService.GetStudentContactsAsync"/> / <see cref="IPeopleLookupService.GetTeacherContactAsync"/>:
/// the batch contact resolution Notifications and Chat consume. Verifies the student's own account,
/// each active guardian, the primary-payer flag, and that a student with no login still resolves
/// (UserId null, e-mail present).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PeopleLookupContactsTests
{
    private readonly FshWebApplicationFactory _factory;

    public PeopleLookupContactsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudentContacts_Resolves_Student_And_Guardians_With_PrimaryPayer_Flag()
    {
        Guid studentWithAccount = Guid.Empty;
        Guid studentNoAccount = Guid.Empty;

        await InTenantScope(async sp =>
        {
            var db = sp.GetRequiredService<PeopleDbContext>();

            var payer = Guardian.Create("Payer", "Pat", "+100", "payer@example.com");
            payer.LinkUser("guardian-user-1");
            var other = Guardian.Create("Other", "Odd", "+101", "other@example.com");
            db.Guardians.AddRange(payer, other);

            var s1 = Student.Create("Linked", "Lee", null, new DateOnly(2012, 1, 1), "+1", "lee@example.com", "mgr", null);
            s1.LinkUser("student-user-1");
            s1.AddGuardianLink(payer.Id, "Parent", isPrimaryPayer: true);
            s1.AddGuardianLink(other.Id, "Aunt", isPrimaryPayer: false);

            var s2 = Student.Create("Loginless", "Sam", null, new DateOnly(2013, 2, 2), "+2", "sam@example.com", "mgr", null);
            s2.AddGuardianLink(payer.Id, "Parent", isPrimaryPayer: true);

            db.Students.AddRange(s1, s2);
            await db.SaveChangesAsync();

            studentWithAccount = s1.Id;
            studentNoAccount = s2.Id;
        });

        await InTenantScope(async sp =>
        {
            var lookup = sp.GetRequiredService<IPeopleLookupService>();

            var contacts = await lookup.GetStudentContactsAsync([studentWithAccount, studentNoAccount]);
            contacts.Count.ShouldBe(2);

            var linked = contacts.Single(c => c.StudentId == studentWithAccount);
            linked.Student.UserId.ShouldBe("student-user-1");
            linked.Student.Role.ShouldBe(ContactRole.Student);
            linked.Guardians.Count.ShouldBe(2);
            linked.Guardians.ShouldContain(g => g.Role == ContactRole.PrimaryPayerGuardian && g.UserId == "guardian-user-1");
            linked.Guardians.ShouldContain(g => g.Role == ContactRole.Guardian && g.UserId == null && g.Email == "other@example.com");

            var loginless = contacts.Single(c => c.StudentId == studentNoAccount);
            loginless.Student.UserId.ShouldBeNull();
            loginless.Student.Email.ShouldBe("sam@example.com");
            loginless.Guardians.Single().Role.ShouldBe(ContactRole.PrimaryPayerGuardian);
        });
    }

    [Fact]
    public async Task GetTeacherContact_Returns_UserId_And_Email_Or_Null()
    {
        Guid teacherId = Guid.Empty;
        await InTenantScope(async sp =>
        {
            var db = sp.GetRequiredService<PeopleDbContext>();
            var teacher = Teacher.Create("Teach", "Tia", null, "+9", "tia@example.com", null, null, null);
            teacher.LinkUser("teacher-user-1");
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
            teacherId = teacher.Id;
        });

        await InTenantScope(async sp =>
        {
            var lookup = sp.GetRequiredService<IPeopleLookupService>();

            var contact = await lookup.GetTeacherContactAsync(teacherId);
            contact.ShouldNotBeNull();
            contact.UserId.ShouldBe("teacher-user-1");
            contact.Email.ShouldBe("tia@example.com");
            contact.Role.ShouldBe(ContactRole.Teacher);

            (await lookup.GetTeacherContactAsync(Guid.NewGuid())).ShouldBeNull();
        });
    }

    private async Task InTenantScope(Func<IServiceProvider, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        await action(scope.ServiceProvider);
    }
}
