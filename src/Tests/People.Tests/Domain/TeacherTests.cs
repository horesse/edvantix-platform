using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Domain;

namespace People.Tests.Domain;

public sealed class TeacherTests
{
    private static Teacher CreateValidTeacher(string[]? specializations = null, decimal? hourlyRate = 25m)
        => Teacher.Create(
            lastName: " Petrov ",
            firstName: " Ivan ",
            middleName: null,
            phone: "+15550001",
            email: "ivan@example.com",
            bio: " Math teacher ",
            specializations: specializations ?? ["Math", "Physics"],
            hourlyRate: hourlyRate);

    [Fact]
    public void Create_Should_SetActiveStatus_When_Created()
    {
        Teacher teacher = CreateValidTeacher();

        teacher.Status.ShouldBe(TeacherStatus.Active);
    }

    [Fact]
    public void Create_Should_Throw_When_HourlyRateIsNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CreateValidTeacher(hourlyRate: -1m));
    }

    [Fact]
    public void GetSpecializations_Should_RoundTrip_ThroughCsvColumn()
    {
        Teacher teacher = CreateValidTeacher(specializations: ["Math", "Chemistry"]);

        teacher.GetSpecializations().ShouldBe(["Math", "Chemistry"]);
    }

    [Fact]
    public void GetSpecializations_Should_ReturnEmpty_When_NoneProvided()
    {
        Teacher teacher = CreateValidTeacher(specializations: []);

        teacher.GetSpecializations().ShouldBeEmpty();
    }

    [Fact]
    public void Deactivate_Should_SetInactiveStatus_When_Active()
    {
        Teacher teacher = CreateValidTeacher();

        teacher.Deactivate();

        teacher.Status.ShouldBe(TeacherStatus.Inactive);
    }

    [Fact]
    public void Activate_Should_SetActiveStatus_When_Inactive()
    {
        Teacher teacher = CreateValidTeacher();
        teacher.Deactivate();

        teacher.Activate();

        teacher.Status.ShouldBe(TeacherStatus.Active);
    }

    [Fact]
    public void Update_Should_Throw_When_HourlyRateIsNegative()
    {
        Teacher teacher = CreateValidTeacher();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            teacher.Update("Petrov", "Ivan", null, "+15550001", "ivan@example.com", null, null, -5m));
    }
}
