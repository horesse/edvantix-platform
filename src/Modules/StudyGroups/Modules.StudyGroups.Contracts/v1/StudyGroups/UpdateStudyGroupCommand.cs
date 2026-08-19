using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

/// <summary><c>Code</c> is not updatable — it is the stable business key referenced by
/// Scheduling/Payments and shown on invoices/chat channel names.</summary>
public sealed record UpdateStudyGroupCommand(
    Guid StudyGroupId,
    string Name,
    Guid PrimaryTeacherId,
    GroupFormat Format,
    int Capacity,
    DateOnly StartDate,
    DateOnly? EndDate = null,
    string? MeetingUrl = null,
    Guid? RoomId = null,
    string? Notes = null) : ICommand<Unit>;
