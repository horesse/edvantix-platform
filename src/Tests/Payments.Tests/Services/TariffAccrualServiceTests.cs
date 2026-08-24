using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Payments.Services;
using FSH.Modules.Scheduling.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace Payments.Tests.Services;

public sealed class TariffAccrualServiceTests
{
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly DateOnly PeriodFrom = new(2026, 9, 1);
    private static readonly DateOnly PeriodTo = new(2026, 9, 30);
    private static readonly AttendanceBreakdown EmptyBreakdown = new(0, 0, 0, 0, 0);

    private static GroupEnrollmentAccrualDto FullPeriodEnrollment(decimal discountPercent = 0m) =>
        new(Guid.NewGuid(), PeriodFrom, null, null, discountPercent);

    #region OneTime / PerPackage — flat, discount-aware

    [Fact]
    public async Task CalculateAsync_OneTime_Should_ChargeFlatAmount()
    {
        var tariff = Tariff.Create("Trial lesson", null, TariffKind.OneTime, 50m, "USD", 0, 0, false);
        var service = new TariffAccrualService(new FakeSessionPlanQueryService(new()), new FakeAttendanceQueryService(EmptyBreakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        line.Quantity.ShouldBe(1m);
        line.UnitPrice.ShouldBe(50m);
    }

    [Fact]
    public async Task CalculateAsync_PerPackage_Should_ApplyDiscount()
    {
        var tariff = Tariff.Create("10-lesson package", null, TariffKind.PerPackage, 200m, "USD", 10, 60, false);
        var service = new TariffAccrualService(new FakeSessionPlanQueryService(new()), new FakeAttendanceQueryService(EmptyBreakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(discountPercent: 10m), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        // 200 * (1 - 10/100) = 180
        line.UnitPrice.ShouldBe(180m);
    }

    #endregion

    #region PerLesson — chargeable count, ChargeOnExcusedAbsence

    [Fact]
    public async Task CalculateAsync_PerLesson_Should_ExcludeExcused_When_FlagIsFalse()
    {
        var tariff = Tariff.Create("Per lesson", null, TariffKind.PerLesson, 10m, "USD", 0, 0, chargeOnExcusedAbsence: false);
        var breakdown = new AttendanceBreakdown(Present: 6, Absent: 2, Late: 0, Excused: 2, Total: 10);
        var service = new TariffAccrualService(new FakeSessionPlanQueryService(new()), new FakeAttendanceQueryService(breakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        line.Quantity.ShouldBe(8m); // 10 total - 2 excused
        line.UnitPrice.ShouldBe(10m);
    }

    [Fact]
    public async Task CalculateAsync_PerLesson_Should_IncludeExcused_When_FlagIsTrue()
    {
        var tariff = Tariff.Create("Per lesson", null, TariffKind.PerLesson, 10m, "USD", 0, 0, chargeOnExcusedAbsence: true);
        var breakdown = new AttendanceBreakdown(Present: 6, Absent: 2, Late: 0, Excused: 2, Total: 10);
        var service = new TariffAccrualService(new FakeSessionPlanQueryService(new()), new FakeAttendanceQueryService(breakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        line.Quantity.ShouldBe(10m);
    }

    [Fact]
    public async Task CalculateAsync_PerLesson_Should_ReturnNull_When_NothingChargeable()
    {
        var tariff = Tariff.Create("Per lesson", null, TariffKind.PerLesson, 10m, "USD", 0, 0, chargeOnExcusedAbsence: false);
        var breakdown = new AttendanceBreakdown(Present: 0, Absent: 0, Late: 0, Excused: 3, Total: 3);
        var service = new TariffAccrualService(new FakeSessionPlanQueryService(new()), new FakeAttendanceQueryService(breakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldBeNull();
    }

    #endregion

    #region PerMonth — proportional accrual

    [Fact]
    public async Task CalculateAsync_PerMonth_Should_ChargeFullAmount_When_EnrolledWholePeriod()
    {
        var tariff = Tariff.Create("Monthly", null, TariffKind.PerMonth, 300m, "USD", 0, 0, false);
        var sessionPlan = new FakeSessionPlanQueryService(new Dictionary<(DateOnly, DateOnly), int> { [(PeriodFrom, PeriodTo)] = 12 });
        var service = new TariffAccrualService(sessionPlan, new FakeAttendanceQueryService(EmptyBreakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        line.UnitPrice.ShouldBe(300m);
    }

    [Fact]
    public async Task CalculateAsync_PerMonth_Should_Prorate_When_EnrolledMidPeriod()
    {
        var tariff = Tariff.Create("Monthly", null, TariffKind.PerMonth, 300m, "USD", 0, 0, false);
        var enrolledOn = new DateOnly(2026, 9, 16);
        var enrollment = new GroupEnrollmentAccrualDto(Guid.NewGuid(), enrolledOn, null, null, 0m);

        var sessionPlan = new FakeSessionPlanQueryService(new Dictionary<(DateOnly, DateOnly), int>
        {
            [(PeriodFrom, PeriodTo)] = 12,
            [(enrolledOn, PeriodTo)] = 6,
        });
        var service = new TariffAccrualService(sessionPlan, new FakeAttendanceQueryService(EmptyBreakdown));

        var line = await service.CalculateAsync(tariff, enrollment, StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        // 300 * 6/12 = 150
        line.UnitPrice.ShouldBe(150m);
    }

    [Fact]
    public async Task CalculateAsync_PerMonth_Should_ChargeFullAmount_When_NoSessionsPlannedYet()
    {
        var tariff = Tariff.Create("Monthly", null, TariffKind.PerMonth, 300m, "USD", 0, 0, false);
        var sessionPlan = new FakeSessionPlanQueryService(new Dictionary<(DateOnly, DateOnly), int> { [(PeriodFrom, PeriodTo)] = 0 });
        var service = new TariffAccrualService(sessionPlan, new FakeAttendanceQueryService(EmptyBreakdown));

        var line = await service.CalculateAsync(tariff, FullPeriodEnrollment(), StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldNotBeNull();
        line.UnitPrice.ShouldBe(300m);
    }

    [Fact]
    public async Task CalculateAsync_PerMonth_Should_ReturnNull_When_EnrollmentLeftBeforePeriod()
    {
        var tariff = Tariff.Create("Monthly", null, TariffKind.PerMonth, 300m, "USD", 0, 0, false);
        var enrollment = new GroupEnrollmentAccrualDto(Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15), null, 0m);
        var service = new TariffAccrualService(new FakeSessionPlanQueryService(new()), new FakeAttendanceQueryService(EmptyBreakdown));

        var line = await service.CalculateAsync(tariff, enrollment, StudyGroupId, PeriodFrom, PeriodTo);

        line.ShouldBeNull();
    }

    #endregion

    private sealed class FakeSessionPlanQueryService(Dictionary<(DateOnly From, DateOnly To), int> counts) : ISessionPlanQueryService
    {
        public ValueTask<int> CountPlannedSessionsAsync(
            Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(counts.GetValueOrDefault((from, toDate)));
    }

    private sealed class FakeAttendanceQueryService(AttendanceBreakdown breakdown) : IAttendanceQueryService
    {
        public ValueTask<int> CountHeldSessionsAsync(
            Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(breakdown.Total);

        public ValueTask<AttendanceBreakdown> GetBreakdownAsync(
            Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(breakdown);
    }
}
