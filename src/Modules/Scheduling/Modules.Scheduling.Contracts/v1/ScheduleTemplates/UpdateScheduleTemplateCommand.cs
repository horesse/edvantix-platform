using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

public sealed record UpdateScheduleTemplateCommand(
    Guid ScheduleTemplateId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    Guid? RoomId,
    Guid? TeacherId,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive) : ICommand<Unit>;
