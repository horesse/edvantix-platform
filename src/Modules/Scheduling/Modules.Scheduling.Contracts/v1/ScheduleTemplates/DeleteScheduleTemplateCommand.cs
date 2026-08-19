using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

public sealed record DeleteScheduleTemplateCommand(Guid ScheduleTemplateId) : ICommand<Unit>;
