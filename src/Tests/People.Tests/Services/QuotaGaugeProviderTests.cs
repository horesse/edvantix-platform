using FSH.Framework.Shared.Quota;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Domain;
using FSH.Modules.People.Services;
using People.Tests.Data;
using Shouldly;
using Xunit;

namespace People.Tests.Services;

public sealed class QuotaGaugeProviderTests
{
    private const string Tenant = "tenant-acme";

    [Fact]
    public async Task ActiveStudents_counts_lead_active_paused_and_excludes_archived()
    {
        await using var db = TestPeopleDbContextFactory.Create(Tenant);

        db.Students.Add(NewStudent("Lead"));                       // Lead
        db.Students.Add(SetStatus(NewStudent("Active"), StudentStatus.Active));
        db.Students.Add(SetStatus(NewStudent("Paused"), StudentStatus.Active, StudentStatus.Paused));
        db.Students.Add(SetStatus(NewStudent("Archived"), StudentStatus.Active, StudentStatus.Archived));
        await db.SaveChangesAsync();

        var sut = new ActiveStudentCountQuotaGaugeProvider(db);
        sut.Resource.ShouldBe(QuotaResource.ActiveStudents);
        (await sut.GetCurrentAsync(Tenant)).ShouldBe(3);
    }

    [Fact]
    public async Task ActiveStudents_ignores_other_tenants()
    {
        await using var db = TestPeopleDbContextFactory.Create(Tenant);
        db.Students.Add(NewStudent("Mine"));
        await db.SaveChangesAsync();

        (await new ActiveStudentCountQuotaGaugeProvider(db).GetCurrentAsync("someone-else")).ShouldBe(0);
    }

    [Fact]
    public async Task ActiveTeachers_counts_only_active()
    {
        await using var db = TestPeopleDbContextFactory.Create(Tenant);

        db.Teachers.Add(NewTeacher("A"));
        db.Teachers.Add(NewTeacher("B"));
        var inactive = NewTeacher("C");
        inactive.Deactivate();
        db.Teachers.Add(inactive);
        await db.SaveChangesAsync();

        var sut = new ActiveTeacherCountQuotaGaugeProvider(db);
        sut.Resource.ShouldBe(QuotaResource.ActiveTeachers);
        (await sut.GetCurrentAsync(Tenant)).ShouldBe(2);
    }

    private static Student NewStudent(string tag) => Student.Create(
        tag, "Test", null, new DateOnly(2010, 1, 1), "+10000000000",
        $"{tag}-{Guid.NewGuid():N}@example.com", Guid.NewGuid().ToString(), null);

    private static Student SetStatus(Student s, params StudentStatus[] transitions)
    {
        foreach (var status in transitions)
        {
            s.ChangeStatus(status);
        }
        return s;
    }

    private static Teacher NewTeacher(string tag) => Teacher.Create(
        tag, "Test", null, "+10000000001", $"{tag}-{Guid.NewGuid():N}@example.com", null, null, null);
}
