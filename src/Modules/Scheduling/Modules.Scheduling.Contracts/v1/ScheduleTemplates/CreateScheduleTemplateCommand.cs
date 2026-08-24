using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

public sealed record CreateScheduleTemplateCommand(
    Guid StudyGroupId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    Guid? RoomId,
    Guid? TeacherId,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : ICommand<Guid>;
