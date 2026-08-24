using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.CreateScheduleTemplate;

public sealed class CreateScheduleTemplateCommandHandler(
    SchedulingDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService)
    : ICommandHandler<CreateScheduleTemplateCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateScheduleTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = await studyGroupQueryService.GetBriefAsync(command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"StudyGroup {command.StudyGroupId} not found.");

        var template = ScheduleTemplate.Create(
            command.StudyGroupId,
            command.DayOfWeek,
            command.StartTime,
            command.DurationMinutes,
            command.RoomId,
            command.TeacherId,
            command.ValidFrom,
            command.ValidTo);

        dbContext.ScheduleTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return template.Id;
    }
}
