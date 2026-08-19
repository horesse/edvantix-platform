using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Calendar;

public sealed record RemoveNonWorkingDayCommand(Guid NonWorkingDayId) : ICommand<Unit>;
