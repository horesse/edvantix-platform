using System.Collections.ObjectModel;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Queries;

/// <summary>
/// Lists the channels the caller belongs to. <paramref name="Kind"/> optionally narrows the result
/// by channel context: <see cref="ChannelKindFilter.StudyGroup"/> keeps only channels that back a
/// study group (<c>SourceStudyGroupId</c> set), <see cref="ChannelKindFilter.Standalone"/> keeps
/// only the others (DMs and ad-hoc channels). Null / empty = no filter.
/// </summary>
public sealed record ListMyChannelsQuery(int Page = 1, int PageSize = 50, string? Kind = null)
    : IQuery<ReadOnlyCollection<ChannelDto>>;

/// <summary>Accepted values for <see cref="ListMyChannelsQuery.Kind"/> (wire contract — kebab-case).</summary>
public static class ChannelKindFilter
{
    public const string StudyGroup = "study-group";
    public const string Standalone = "standalone";
}
