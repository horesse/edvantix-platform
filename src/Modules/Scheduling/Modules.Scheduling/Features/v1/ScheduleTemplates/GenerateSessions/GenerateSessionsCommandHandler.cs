using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.GenerateSessions;

public sealed class GenerateSessionsCommandHandler(
    SchedulingDbContext dbContext,
    IScheduleGeneratorService generatorService,
    IStudyGroupQueryService studyGroupQueryService)
    : ICommandHandler<GenerateSessionsCommand, GenerationResultDto>
{
    public async ValueTask<GenerationResultDto> Handle(GenerateSessionsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await dbContext.ScheduleTemplates
            .FirstOrDefaultAsync(t => t.Id == command.ScheduleTemplateId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ScheduleTemplate {command.ScheduleTemplateId} not found.");

        var plan = await generatorService.PlanAsync(
                template, command.HorizonWeeks ?? SchedulingDefaults.DefaultHorizonWeeks, cancellationToken)
            .ConfigureAwait(false);

        // TeacherId is resolved the same way PlanAsync resolved it for conflict-checking (template
        // override, else the group's primary teacher) — recomputed here (once, not per occurrence)
        // since Session.Create needs it and the plan only carries times.
        Guid teacherId;
        if (template.TeacherId is { } overrideTeacherId)
        {
            teacherId = overrideTeacherId;
        }
        else
        {
            var group = await studyGroupQueryService.GetBriefAsync(template.StudyGroupId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException($"StudyGroup {template.StudyGroupId} not found.");
            teacherId = group.PrimaryTeacherId;
        }

        var createdIds = new List<Guid>(plan.ToCreate.Count);
        foreach (var occurrence in plan.ToCreate)
        {
            var session = Session.Create(
                template.StudyGroupId,
                lessonId: null,
                teacherId,
                roomId: template.RoomId,
                occurrence.StartUtc,
                occurrence.EndUtc,
                topic: null,
                meetingUrl: null,
                scheduleTemplateId: template.Id);

            dbContext.Sessions.Add(session);
            createdIds.Add(session.Id);
        }

        if (createdIds.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new GenerationResultDto(
            template.Id,
            createdIds,
            plan.Skipped.Select(s => new GenerationSkipDto(s.LocalDate, s.Reason, s.Conflicts)).ToList());
    }
}
