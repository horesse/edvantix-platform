namespace FSH.Modules.Chat.Contracts.v1.DTOs;

public sealed record ChannelDto(
    Guid Id,
    ChannelType Type,
    string? Name,
    string? Slug,
    string? Description,
    bool IsPrivate,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    IReadOnlyList<ChannelMemberDto> Members,
    // Set when the channel backs a study group (provisioned from StudyGroupCreatedIntegrationEvent).
    // Null for user-created channels and DMs — lets the SPA tell group channels apart from ad-hoc
    // ones and deep-link to the group.
    Guid? SourceStudyGroupId = null);
