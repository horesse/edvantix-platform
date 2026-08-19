using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates;

internal static class ScheduleTemplateMappings
{
    public static ScheduleTemplateDto ToDto(this ScheduleTemplate t) => new(
        t.Id,
        t.StudyGroupId,
        t.DayOfWeek,
        t.StartTime,
        t.DurationMinutes,
        t.RoomId,
        t.TeacherId,
        t.ValidFrom,
        t.ValidTo,
        t.IsActive);
}
