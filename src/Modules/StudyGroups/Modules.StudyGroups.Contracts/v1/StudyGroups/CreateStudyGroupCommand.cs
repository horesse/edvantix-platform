using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

/// <summary>Creates a study group in <see cref="Dtos.StudyGroupStatus.Forming"/>. The handler
/// checks <c>Code</c> uniqueness within the tenant and <c>ICourseQueryService.IsPublishedAsync</c>
/// (see docs/02 Модули/StudyGroups.md → Инварианты).</summary>
public sealed record CreateStudyGroupCommand(
    string Code,
    string Name,
    Guid CourseId,
    Guid PrimaryTeacherId,
    GroupFormat Format,
    int Capacity,
    DateOnly StartDate,
    DateOnly? EndDate = null,
    string? MeetingUrl = null,
    Guid? RoomId = null,
    string? Notes = null) : ICommand<Guid>;
