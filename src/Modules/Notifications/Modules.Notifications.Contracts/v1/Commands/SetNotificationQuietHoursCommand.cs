using Mediator;

namespace FSH.Modules.Notifications.Contracts.v1.Commands;

/// <summary>Sets the school-wide quiet-hours window. <c>StartLocal</c> &gt; <c>EndLocal</c> spans midnight.</summary>
public sealed record SetNotificationQuietHoursCommand(bool Enabled, TimeOnly StartLocal, TimeOnly EndLocal) : ICommand<Unit>;
