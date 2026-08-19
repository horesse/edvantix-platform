using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

/// <summary>Groups for the caller: as teacher (<c>PrimaryTeacherId</c> or a <c>GroupTeacher</c>
/// row) or as student (an enrollment, any status) — resolved via
/// <c>IPeopleScopeResolver.ResolveAsync</c> on the current user, same pattern documented for
/// Scheduling/Payments in docs/02 Модули/People.md.</summary>
public sealed record GetMyStudyGroupsQuery : IQuery<IReadOnlyList<StudyGroupDto>>;
