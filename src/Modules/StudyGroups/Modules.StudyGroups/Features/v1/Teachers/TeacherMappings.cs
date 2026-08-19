using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Domain;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers;

internal static class TeacherMappings
{
    public static GroupTeacherDto ToDto(this GroupTeacher t) => new(t.Id, t.StudyGroupId, t.TeacherId, t.Role);
}
