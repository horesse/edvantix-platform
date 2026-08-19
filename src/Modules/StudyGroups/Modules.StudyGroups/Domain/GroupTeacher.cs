using FSH.Framework.Core.Domain;
using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace FSH.Modules.StudyGroups.Domain;

/// <summary>
/// A teacher on a <see cref="StudyGroup"/>'s staffing roster (primary/assistant/substitute) —
/// independent of <see cref="StudyGroup.PrimaryTeacherId"/> (see that property's remarks). Owned by
/// the group, hard-deleted on <see cref="StudyGroup.RemoveTeacher"/> — no historical requirement was
/// specified for staffing changes, unlike <see cref="GroupEnrollment"/>.
/// </summary>
public sealed class GroupTeacher : BaseEntity<Guid>
{
    public Guid StudyGroupId { get; private set; }
    public Guid TeacherId { get; private set; }
    public TeacherRole Role { get; private set; }

    private GroupTeacher() { }

    internal static GroupTeacher Create(Guid studyGroupId, Guid teacherId, TeacherRole role) => new()
    {
        Id = Guid.CreateVersion7(),
        StudyGroupId = studyGroupId,
        TeacherId = teacherId,
        Role = role,
    };
}
