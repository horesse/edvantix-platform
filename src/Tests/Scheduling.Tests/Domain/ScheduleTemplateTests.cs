using FSH.Modules.Scheduling.Domain;

namespace Scheduling.Tests.Domain;

public sealed class ScheduleTemplateTests
{
    private static ScheduleTemplate CreateValidTemplate(
        DayOfWeek dayOfWeek = DayOfWeek.Tuesday,
        DateOnly? validFrom = null,
        DateOnly? validTo = null) => ScheduleTemplate.Create(
        studyGroupId: Guid.NewGuid(),
        dayOfWeek: dayOfWeek,
        startTime: new TimeOnly(18, 0),
        durationMinutes: 60,
        roomId: null,
        teacherId: null,
        validFrom: validFrom ?? new DateOnly(2026, 9, 1),
        validTo: validTo);

    #region Create

    [Fact]
    public void Create_Should_SetActive_When_Created()
    {
        var template = CreateValidTemplate();

        template.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_Should_Throw_When_DurationIsZero()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ScheduleTemplate.Create(
            Guid.NewGuid(), DayOfWeek.Tuesday, new TimeOnly(18, 0), durationMinutes: 0,
            null, null, new DateOnly(2026, 9, 1), null));
    }

    [Fact]
    public void Create_Should_Throw_When_ValidToBeforeValidFrom()
    {
        Should.Throw<ArgumentException>(() => ScheduleTemplate.Create(
            Guid.NewGuid(), DayOfWeek.Tuesday, new TimeOnly(18, 0), 60,
            null, null, validFrom: new DateOnly(2026, 9, 1), validTo: new DateOnly(2026, 8, 1)));
    }

    #endregion

    #region AppliesOn

    [Fact]
    public void AppliesOn_Should_ReturnTrue_When_DateMatchesDayOfWeekAndInRange()
    {
        var template = CreateValidTemplate(DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), null);

        // 2026-09-08 is a Tuesday.
        template.AppliesOn(new DateOnly(2026, 9, 8)).ShouldBeTrue();
    }

    [Fact]
    public void AppliesOn_Should_ReturnFalse_When_DayOfWeekDoesNotMatch()
    {
        var template = CreateValidTemplate(DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), null);

        // 2026-09-09 is a Wednesday.
        template.AppliesOn(new DateOnly(2026, 9, 9)).ShouldBeFalse();
    }

    [Fact]
    public void AppliesOn_Should_ReturnFalse_When_BeforeValidFrom()
    {
        var template = CreateValidTemplate(DayOfWeek.Tuesday, new DateOnly(2026, 9, 8), null);

        // 2026-09-01 is a Tuesday, but before ValidFrom.
        template.AppliesOn(new DateOnly(2026, 9, 1)).ShouldBeFalse();
    }

    [Fact]
    public void AppliesOn_Should_ReturnFalse_When_AfterValidTo()
    {
        var template = CreateValidTemplate(DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 8));

        // 2026-09-15 is a Tuesday, but after ValidTo.
        template.AppliesOn(new DateOnly(2026, 9, 15)).ShouldBeFalse();
    }

    [Fact]
    public void AppliesOn_Should_ReturnFalse_When_Inactive()
    {
        var template = CreateValidTemplate(DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), null);
        template.Update(
            template.DayOfWeek, template.StartTime, template.DurationMinutes,
            template.RoomId, template.TeacherId, template.ValidFrom, template.ValidTo, isActive: false);

        template.AppliesOn(new DateOnly(2026, 9, 8)).ShouldBeFalse();
    }

    #endregion
}
