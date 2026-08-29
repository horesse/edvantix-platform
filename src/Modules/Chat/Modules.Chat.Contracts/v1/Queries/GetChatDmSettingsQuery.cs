using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Queries;

/// <summary>The school's direct-message policy toggles.</summary>
public sealed record GetChatDmSettingsQuery : IQuery<ChatDmSettingsDto>;
