using FluentValidation;
using FSH.Modules.Chat.Contracts.v1.Queries;

namespace FSH.Modules.Chat.Features.v1.Channels.ListMyChannels;

public sealed class ListMyChannelsQueryValidator : AbstractValidator<ListMyChannelsQuery>
{
    public ListMyChannelsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Kind)
            .Must(k => string.IsNullOrWhiteSpace(k)
                || k == ChannelKindFilter.StudyGroup
                || k == ChannelKindFilter.Standalone)
            .WithMessage($"Kind must be '{ChannelKindFilter.StudyGroup}', '{ChannelKindFilter.Standalone}', or empty.");
    }
}
