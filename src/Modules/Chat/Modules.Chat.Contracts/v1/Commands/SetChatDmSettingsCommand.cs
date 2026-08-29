using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

/// <summary>Sets the school's direct-message policy toggles.</summary>
public sealed record SetChatDmSettingsCommand(bool AllowStudentToStudentDm) : ICommand<Unit>;
