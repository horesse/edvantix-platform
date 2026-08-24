using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Scheduling.Contracts;
using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace FSH.Modules.Payments.Services;

public sealed class TariffAccrualService(
    ISessionPlanQueryService sessionPlanQueryService,
    IAttendanceQueryService attendanceQueryService) : ITariffAccrualService
{
    public ValueTask<AccrualLine?> CalculateAsync(
        Tariff tariff,
        GroupEnrollmentAccrualDto enrollment,
        Guid studyGroupId,
        DateOnly periodFrom,
        DateOnly periodTo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tariff);
        ArgumentNullException.ThrowIfNull(enrollment);

        decimal discountMultiplier = 1m - (enrollment.DiscountPercent / 100m);

        return tariff.Kind switch
        {
            TariffKind.OneTime => ValueTask.FromResult<AccrualLine?>(
                new AccrualLine(tariff.Name, 1, Round(tariff.Amount * discountMultiplier))),

            // "Предоплата за N занятий" — the invoice charges for the whole package once; the
            // remaining-sessions balance is a read-side projection (GetStudentBalanceQuery), not
            // computed here.
            TariffKind.PerPackage => ValueTask.FromResult<AccrualLine?>(
                new AccrualLine($"{tariff.Name} ({tariff.LessonsCount} занятий)", 1, Round(tariff.Amount * discountMultiplier))),

            TariffKind.PerLesson => CalculatePerLessonAsync(tariff, enrollment, studyGroupId, periodFrom, periodTo, discountMultiplier, cancellationToken),
            TariffKind.PerMonth => CalculatePerMonthAsync(tariff, enrollment, studyGroupId, periodFrom, periodTo, discountMultiplier, cancellationToken),
            _ => ValueTask.FromResult<AccrualLine?>(null),
        };
    }

    /// <summary>Sum × number of <c>Held</c> sessions the student is on record for, minus excused
    /// absences unless <see cref="Tariff.ChargeOnExcusedAbsence"/> — cancelled sessions never reach
    /// this count at all (Scheduling excludes them from attendance rows in the first place).</summary>
    private async ValueTask<AccrualLine?> CalculatePerLessonAsync(
        Tariff tariff, GroupEnrollmentAccrualDto enrollment, Guid studyGroupId,
        DateOnly periodFrom, DateOnly periodTo, decimal discountMultiplier, CancellationToken cancellationToken)
    {
        var breakdown = await attendanceQueryService
            .GetBreakdownAsync(enrollment.StudentId, studyGroupId, periodFrom, periodTo, cancellationToken)
            .ConfigureAwait(false);

        int chargeable = breakdown.Total - (tariff.ChargeOnExcusedAbsence ? 0 : breakdown.Excused);
        if (chargeable <= 0)
        {
            return null;
        }

        return new AccrualLine($"{tariff.Name} — {chargeable} занятий", chargeable, Round(tariff.Amount * discountMultiplier));
    }

    /// <summary>Flat monthly sum, prorated to the fraction of the period's planned sessions the
    /// enrollment actually overlaps — "при зачислении/отчислении посреди месяца, пропорционально
    /// числу запланированных занятий" (see docs/02 Модули/Payments.md → «Модель начисления»).</summary>
    private async ValueTask<AccrualLine?> CalculatePerMonthAsync(
        Tariff tariff, GroupEnrollmentAccrualDto enrollment, Guid studyGroupId,
        DateOnly periodFrom, DateOnly periodTo, decimal discountMultiplier, CancellationToken cancellationToken)
    {
        var overlapFrom = enrollment.EnrolledOn > periodFrom ? enrollment.EnrolledOn : periodFrom;
        var overlapTo = enrollment.LeftOn is { } leftOn && leftOn < periodTo ? leftOn : periodTo;
        if (overlapFrom > overlapTo)
        {
            return null;
        }

        bool fullPeriod = overlapFrom == periodFrom && overlapTo == periodTo;
        int totalPlanned = await sessionPlanQueryService
            .CountPlannedSessionsAsync(studyGroupId, periodFrom, periodTo, cancellationToken)
            .ConfigureAwait(false);

        if (totalPlanned <= 0)
        {
            // Nothing scheduled at all this period (yet) — bill the flat amount rather than 0/0.
            return new AccrualLine(tariff.Name, 1, Round(tariff.Amount * discountMultiplier));
        }

        int enrolledPlanned = fullPeriod
            ? totalPlanned
            : await sessionPlanQueryService.CountPlannedSessionsAsync(studyGroupId, overlapFrom, overlapTo, cancellationToken).ConfigureAwait(false);

        if (enrolledPlanned <= 0)
        {
            return null;
        }

        decimal amount = Round(tariff.Amount * discountMultiplier * enrolledPlanned / totalPlanned);
        string description = fullPeriod ? tariff.Name : $"{tariff.Name} (пропорционально: {enrolledPlanned}/{totalPlanned})";
        return new AccrualLine(description, 1, amount);
    }

    private static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
