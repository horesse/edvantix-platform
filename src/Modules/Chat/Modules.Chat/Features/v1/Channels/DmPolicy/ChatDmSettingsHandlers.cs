using FluentValidation;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Contracts.v1.Queries;
using FSH.Modules.Multitenancy.Contracts.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Chat.Features.v1.Channels.DmPolicy;

public sealed class GetChatDmSettingsQueryHandler(IChatDmSettingsService service)
    : IQueryHandler<GetChatDmSettingsQuery, ChatDmSettingsDto>
{
    public async ValueTask<ChatDmSettingsDto> Handle(GetChatDmSettingsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await service.GetAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class SetChatDmSettingsCommandHandler(IChatDmSettingsService service)
    : ICommandHandler<SetChatDmSettingsCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetChatDmSettingsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await service.SetAsync(command.AllowStudentToStudentDm, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class SetChatDmSettingsCommandValidator : AbstractValidator<SetChatDmSettingsCommand>
{
    // Single boolean — nothing to validate; present so the arch test's command↔validator pairing holds.
}

public static class ChatDmSettingsEndpoints
{
    internal static RouteHandlerBuilder MapGetChatDmSettingsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/dm-settings",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetChatDmSettingsQuery(), ct)))
            .WithName("GetChatDmSettings")
            .WithSummary("The school's direct-message policy toggles")
            .RequirePermission(MultitenancyPermissions.SchoolSettings.View);

    internal static RouteHandlerBuilder MapSetChatDmSettingsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/dm-settings",
                async (SetChatDmSettingsCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetChatDmSettings")
            .WithSummary("Set the school's direct-message policy toggles")
            .RequirePermission(MultitenancyPermissions.SchoolSettings.Manage);
}
