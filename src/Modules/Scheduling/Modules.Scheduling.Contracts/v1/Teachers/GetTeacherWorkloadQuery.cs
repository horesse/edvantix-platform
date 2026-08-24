using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Teachers;

/// <summary>Null <see cref="From"/>/<see cref="To"/> default to "today through +7 days" in the
/// handler — a near-term view, not the 8-week generation horizon.</summary>
public sealed record GetTeacherWorkloadQuery(Guid TeacherId, DateOnly? From, DateOnly? To) : IQuery<TeacherWorkloadDto>;
