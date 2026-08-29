using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Chat.Contracts.Events;

/// <summary>
/// Published by Chat after it provisions the private channel for a newly created study group.
/// Consumed by StudyGroups to fill <c>StudyGroup.ChatChannelId</c> — Chat cannot reference the
/// StudyGroups runtime, so the link is closed with an event rather than a direct call.
/// </summary>
public sealed record StudyGroupChannelLinkedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudyGroupId,
    Guid ChannelId) : IIntegrationEvent;
