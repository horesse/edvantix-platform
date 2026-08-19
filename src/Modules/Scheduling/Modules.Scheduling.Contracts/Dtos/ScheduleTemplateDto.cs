namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record ScheduleTemplateDto(
    Guid Id,
    Guid StudyGroupId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    Guid? RoomId,
    Guid? TeacherId,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive);
