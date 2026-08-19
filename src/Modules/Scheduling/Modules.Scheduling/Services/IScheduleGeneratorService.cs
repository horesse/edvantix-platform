using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Services;

/// <summary>Computes what <c>GenerateSessionsCommand</c>/<c>PreviewGenerationQuery</c> need — kept
/// separate from the Mediator handlers so both share one implementation of the timezone-aware
/// expansion (see docs/02 Модули/Scheduling.md → "Время"/"Генерация"). <see cref="PlanAsync"/> never
/// writes to the database; the caller (the command handler) persists <see cref="ScheduleGenerationPlan.ToCreate"/>.</summary>
public interface IScheduleGeneratorService
{
    ValueTask<ScheduleGenerationPlan> PlanAsync(
        ScheduleTemplate scheduleTemplate, int horizonWeeks, CancellationToken cancellationToken = default);
}

/// <param name="ToCreate">Occurrences with no conflict and no existing <see cref="Session"/> row yet
/// — safe to insert as-is.</param>
/// <param name="Skipped">Occurrences that hit a non-working day or a resource conflict.</param>
public sealed record ScheduleGenerationPlan(
    IReadOnlyList<PlannedOccurrence> ToCreate,
    IReadOnlyList<SkippedOccurrence> Skipped);

public sealed record PlannedOccurrence(DateOnly LocalDate, DateTimeOffset StartUtc, DateTimeOffset EndUtc);

public sealed record SkippedOccurrence(
    DateOnly LocalDate,
    GenerationSkipReason Reason,
    IReadOnlyList<SessionConflictDto> Conflicts);
