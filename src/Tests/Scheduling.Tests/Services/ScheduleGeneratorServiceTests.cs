using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using NSubstitute;

namespace Scheduling.Tests.Services;

public sealed class ScheduleGeneratorServiceTests
{
    #region DST — mandatory per docs/02 Модули/Scheduling.md → "Время"

    /// <summary>
    /// US DST spring-forward 2030: clocks jump 02:00 -> 03:00 on Sunday 2030-03-10 (America/New_York,
    /// EST -05:00 -> EDT -04:00). A template's local "18:00 every Tuesday" must land at 18:00 local
    /// on BOTH sides of that transition — i.e. the UTC instant must shift by exactly one hour, not
    /// stay fixed (which is what naively adding 7 days to a UTC timestamp would produce).
    /// </summary>
    [Fact]
    public void ToUtc_Should_KeepLocalWallClockTime_AcrossDstSpringForwardTransition()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var localTime = new TimeOnly(18, 0);

        var beforeTransition = ScheduleGeneratorService.ToUtc(new DateOnly(2030, 3, 5), localTime, timeZone); // Tue, EST
        var afterTransition = ScheduleGeneratorService.ToUtc(new DateOnly(2030, 3, 12), localTime, timeZone); // Tue, EDT

        // Local wall-clock time must read 18:00 on both sides.
        TimeZoneInfo.ConvertTimeFromUtc(beforeTransition.UtcDateTime, timeZone).TimeOfDay.ShouldBe(localTime.ToTimeSpan());
        TimeZoneInfo.ConvertTimeFromUtc(afterTransition.UtcDateTime, timeZone).TimeOfDay.ShouldBe(localTime.ToTimeSpan());

        // Both instants are expressed as UTC (Offset == 0 by construction — ToUtc always returns a
        // UTC-flagged DateTimeOffset). The DST-awareness proof is in the UTC clock hour: EST is
        // UTC-5, EDT is UTC-4, so 18:00 local lands at 23:00 UTC before the transition and 22:00 UTC
        // after — a one-hour shift, not the 7-day-exact gap a naive "add 7 days to UTC" would give.
        beforeTransition.Hour.ShouldBe(23);
        afterTransition.Hour.ShouldBe(22);
        (afterTransition - beforeTransition).ShouldNotBe(TimeSpan.FromDays(7));
        (afterTransition - beforeTransition).ShouldBe(TimeSpan.FromDays(7) - TimeSpan.FromHours(1));
    }

    [Fact]
    public void ToUtc_Should_ProduceSameUtcInstant_When_TimeZoneHasNoDst()
    {
        // UTC itself never shifts — a useful control case alongside the DST test above.
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        var localTime = new TimeOnly(18, 0);

        var week1 = ScheduleGeneratorService.ToUtc(new DateOnly(2030, 3, 5), localTime, timeZone);
        var week2 = ScheduleGeneratorService.ToUtc(new DateOnly(2030, 3, 12), localTime, timeZone);

        (week2 - week1).ShouldBe(TimeSpan.FromDays(7));
    }

    #endregion

    #region PlanAsync

    [Fact]
    public async Task PlanAsync_Should_ReturnEmpty_When_TemplateIsInactive()
    {
        var (service, _, _, _) = CreateService();
        var template = CreateTemplate(isActive: false);

        var plan = await service.PlanAsync(template, horizonWeeks: 4);

        plan.ToCreate.ShouldBeEmpty();
        plan.Skipped.ShouldBeEmpty();
    }

    [Fact]
    public async Task PlanAsync_Should_GenerateOneOccurrencePerWeek_When_NoConflictsOrNonWorkingDays()
    {
        var (service, conflictChecker, studyGroupQueryService, _) = CreateService();
        studyGroupQueryService.GetBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StudyGroupBriefDto(Guid.NewGuid(), "A1-01", "Group", Guid.NewGuid(), Guid.NewGuid(), StudyGroupStatus.Active));
        conflictChecker.CheckAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var validTo = validFrom.AddDays(21); // ~3 weeks, so 3 weekly occurrences at most
        var template = CreateTemplate(validFrom: validFrom, validTo: validTo);

        var plan = await service.PlanAsync(template, horizonWeeks: 10);

        plan.ToCreate.ShouldNotBeEmpty();
        plan.ToCreate.ShouldAllBe(o => o.LocalDate.DayOfWeek == template.DayOfWeek);
    }

    [Fact]
    public async Task PlanAsync_Should_UseGroupPrimaryTeacher_When_TemplateHasNoTeacherOverride()
    {
        var (service, conflictChecker, studyGroupQueryService, _) = CreateService();
        var primaryTeacherId = Guid.NewGuid();
        studyGroupQueryService.GetBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StudyGroupBriefDto(Guid.NewGuid(), "A1-01", "Group", Guid.NewGuid(), primaryTeacherId, StudyGroupStatus.Active));
        conflictChecker.CheckAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var template = CreateTemplate(validFrom: validFrom, validTo: validFrom.AddDays(6), teacherId: null);

        await service.PlanAsync(template, horizonWeeks: 10);

        await conflictChecker.Received().CheckAsync(
            Arg.Any<Guid?>(), primaryTeacherId, Arg.Any<Guid?>(), Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanAsync_Should_SkipNonWorkingDay_And_ReportIt()
    {
        var (service, conflictChecker, studyGroupQueryService, dbContext) = CreateService();
        studyGroupQueryService.GetBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StudyGroupBriefDto(Guid.NewGuid(), "A1-01", "Group", Guid.NewGuid(), Guid.NewGuid(), StudyGroupStatus.Active));
        conflictChecker.CheckAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var template = CreateTemplate(validFrom: validFrom, validTo: validFrom, dayOfWeek: validFrom.DayOfWeek);
        dbContext.NonWorkingDays.Add(NonWorkingDay.Create(validFrom, "Holiday"));
        await dbContext.SaveChangesAsync();

        var plan = await service.PlanAsync(template, horizonWeeks: 10);

        plan.ToCreate.ShouldBeEmpty();
        plan.Skipped.ShouldContain(s => s.Reason == GenerationSkipReason.NonWorkingDay && s.LocalDate == validFrom);
    }

    [Fact]
    public async Task PlanAsync_Should_SkipConflictingOccurrence_And_ReportIt()
    {
        var (service, conflictChecker, studyGroupQueryService, _) = CreateService();
        studyGroupQueryService.GetBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StudyGroupBriefDto(Guid.NewGuid(), "A1-01", "Group", Guid.NewGuid(), Guid.NewGuid(), StudyGroupStatus.Active));

        var conflict = new SessionConflictDto(SessionConflictType.Teacher, Guid.NewGuid(), DateTimeOffset.UtcNow);
        conflictChecker.CheckAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([conflict]);

        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var template = CreateTemplate(validFrom: validFrom, validTo: validFrom, dayOfWeek: validFrom.DayOfWeek);

        var plan = await service.PlanAsync(template, horizonWeeks: 10);

        plan.ToCreate.ShouldBeEmpty();
        plan.Skipped.ShouldContain(s => s.Reason == GenerationSkipReason.Conflict && s.Conflicts.Contains(conflict));
    }

    [Fact]
    public async Task PlanAsync_Should_SkipAlreadyGeneratedOccurrence_Silently()
    {
        var (service, conflictChecker, studyGroupQueryService, dbContext) = CreateService();
        var group = new StudyGroupBriefDto(Guid.NewGuid(), "A1-01", "Group", Guid.NewGuid(), Guid.NewGuid(), StudyGroupStatus.Active);
        studyGroupQueryService.GetBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(group);
        conflictChecker.CheckAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var template = CreateTemplate(validFrom: validFrom, validTo: validFrom, dayOfWeek: validFrom.DayOfWeek);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        var startUtc = ScheduleGeneratorService.ToUtc(validFrom, template.StartTime, timeZone);
        var existing = Session.Create(template.StudyGroupId, null, group.PrimaryTeacherId, null, startUtc, startUtc.AddMinutes(60), null, null, scheduleTemplateId: template.Id);
        dbContext.Sessions.Add(existing);
        await dbContext.SaveChangesAsync();

        var plan = await service.PlanAsync(template, horizonWeeks: 10);

        plan.ToCreate.ShouldBeEmpty();
        plan.Skipped.ShouldBeEmpty(); // already-generated occurrences are omitted, not reported
    }

    #endregion

    private static ScheduleTemplate CreateTemplate(
        bool isActive = true,
        DateOnly? validFrom = null,
        DateOnly? validTo = null,
        DayOfWeek dayOfWeek = DayOfWeek.Tuesday,
        Guid? teacherId = null)
    {
        var template = ScheduleTemplate.Create(
            studyGroupId: Guid.NewGuid(),
            dayOfWeek: dayOfWeek,
            startTime: new TimeOnly(18, 0),
            durationMinutes: 60,
            roomId: null,
            teacherId: teacherId,
            validFrom: validFrom ?? DateOnly.FromDateTime(DateTime.UtcNow),
            validTo: validTo);

        if (!isActive)
        {
            template.Update(
                template.DayOfWeek, template.StartTime, template.DurationMinutes,
                template.RoomId, template.TeacherId, template.ValidFrom, template.ValidTo, isActive: false);
        }

        return template;
    }

    private static (ScheduleGeneratorService Service, ISessionConflictChecker ConflictChecker,
        IStudyGroupQueryService StudyGroupQueryService, FSH.Modules.Scheduling.Data.SchedulingDbContext DbContext) CreateService()
    {
        var dbContext = TestSchedulingDbContextFactory.Create();
        var conflictChecker = Substitute.For<ISessionConflictChecker>();
        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        var tenantSettingsService = Substitute.For<ITenantSettingsService>();
        tenantSettingsService.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantSettingsDto { TimeZoneId = "UTC", Currency = "USD" });

        var service = new ScheduleGeneratorService(dbContext, conflictChecker, tenantSettingsService, studyGroupQueryService);
        return (service, conflictChecker, studyGroupQueryService, dbContext);
    }
}
