using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Calendar;

public sealed record AddNonWorkingDayCommand(DateOnly Date, string? Description) : ICommand<Guid>;
