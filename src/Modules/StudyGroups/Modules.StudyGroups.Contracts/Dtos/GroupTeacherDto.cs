namespace FSH.Modules.StudyGroups.Contracts.Dtos;

public sealed record GroupTeacherDto(
    Guid Id,
    Guid StudyGroupId,
    Guid TeacherId,
    TeacherRole Role);
