using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.StudyGroups.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

public sealed class ScheduleGeneratorService(
    SchedulingDbContext dbContext,
    ISessionConflictChecker conflictChecker,
    ITenantSettingsService tenantSettingsService,
    IStudyGroupQueryService studyGroupQueryService) : IScheduleGeneratorService
{
    public async ValueTask<ScheduleGenerationPlan> PlanAsync(
        ScheduleTemplate scheduleTemplate, int horizonWeeks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduleTemplate);
        if (horizonWeeks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonWeeks), "HorizonWeeks must be positive.");
        }

        var toCreate = new List<PlannedOccurrence>();
        var skipped = new List<SkippedOccurrence>();

        // An inactive template (deactivated on StudyGroupFinishedIntegrationEvent, or by the manager
        // directly) generates nothing — the caller doesn't need a special case for it.
        if (!scheduleTemplate.IsActive)
        {
            return new ScheduleGenerationPlan(toCreate, skipped);
        }

        var settings = await tenantSettingsService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);

        var group = await studyGroupQueryService.GetBriefAsync(scheduleTemplate.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"StudyGroup {scheduleTemplate.StudyGroupId} not found.");

        // TeacherId on the template is an override; falling back to the group's primary teacher is
        // what makes it optional — see docs/02 Модули/Scheduling.md → "Генерация" and
        // StudyGroupBriefDto.PrimaryTeacherId.
        var teacherId = scheduleTemplate.TeacherId ?? group.PrimaryTeacherId;

        var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        var horizonEnd = todayLocal.AddDays(horizonWeeks * 7);
        var rangeStart = todayLocal > scheduleTemplate.ValidFrom ? todayLocal : scheduleTemplate.ValidFrom;
        var rangeEnd = scheduleTemplate.ValidTo is { } validTo && validTo < horizonEnd ? validTo : horizonEnd;

        if (rangeStart > rangeEnd)
        {
            return new ScheduleGenerationPlan(toCreate, skipped);
        }

        var candidateDates = EnumerateOccurrenceDates(rangeStart, rangeEnd, scheduleTemplate.DayOfWeek).ToList();
        if (candidateDates.Count == 0)
        {
            return new ScheduleGenerationPlan(toCreate, skipped);
        }

        var nonWorkingDates = (await dbContext.NonWorkingDays
                .AsNoTracking()
                .Where(d => d.Date >= rangeStart && d.Date <= rangeEnd)
                .Select(d => d.Date)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToHashSet();

        foreach (var localDate in candidateDates)
        {
            if (nonWorkingDates.Contains(localDate))
            {
                skipped.Add(new SkippedOccurrence(localDate, GenerationSkipReason.NonWorkingDay, []));
                continue;
            }

            var startUtc = ToUtc(localDate, scheduleTemplate.StartTime, timeZone);
            var endUtc = startUtc.AddMinutes(scheduleTemplate.DurationMinutes);

            // Idempotency: a prior run already created this exact occurrence — silently omit it from
            // both lists. Re-running the generator over dates it already covered is a no-op, not a
            // "skip" worth reporting (see docs/02 Модули/Scheduling.md → Инварианты).
            bool alreadyExists = await dbContext.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.ScheduleTemplateId == scheduleTemplate.Id && s.StartUtc == startUtc, cancellationToken)
                .ConfigureAwait(false);
            if (alreadyExists)
            {
                continue;
            }

            var conflicts = await conflictChecker.CheckAsync(
                    excludeSessionId: null, teacherId, scheduleTemplate.RoomId, scheduleTemplate.StudyGroupId, startUtc, endUtc, cancellationToken)
                .ConfigureAwait(false);

            if (conflicts.Count > 0)
            {
                skipped.Add(new SkippedOccurrence(localDate, GenerationSkipReason.Conflict, conflicts));
                continue;
            }

            toCreate.Add(new PlannedOccurrence(localDate, startUtc, endUtc));
        }

        return new ScheduleGenerationPlan(toCreate, skipped);
    }

    private static IEnumerable<DateOnly> EnumerateOccurrenceDates(DateOnly start, DateOnly end, DayOfWeek dayOfWeek)
    {
        int offset = ((int)dayOfWeek - (int)start.DayOfWeek + 7) % 7;
        var cursor = start.AddDays(offset);
        while (cursor <= end)
        {
            yield return cursor;
            cursor = cursor.AddDays(7);
        }
    }

    /// <summary>Converts a school-local wall-clock time to UTC via <see cref="TimeZoneInfo"/> —
    /// recalculated per occurrence, not by adding 7 days to a UTC instant, so DST transitions land
    /// correctly (see docs/02 Модули/Scheduling.md → "Время"). Internal (not private) specifically so
    /// <c>Scheduling.Tests</c> can exercise the DST-transition math directly, without threading a
    /// mockable "now" through the whole <see cref="PlanAsync"/> pipeline — see the module's
    /// <c>AssemblyInfo.cs</c> for the <c>InternalsVisibleTo</c> grant.</summary>
    internal static DateTimeOffset ToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo timeZone)
    {
        var localDateTime = localDate.ToDateTime(localTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }
}
