using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Calendar;

/// <summary>Optionally scoped to [<paramref name="From"/>, <paramref name="To"/>] — the generator
/// uses this to skip non-working days in its horizon; the admin screen calls it unfiltered.</summary>
public sealed record GetNonWorkingDaysQuery(DateOnly? From, DateOnly? To) : IQuery<IReadOnlyList<NonWorkingDayDto>>;
