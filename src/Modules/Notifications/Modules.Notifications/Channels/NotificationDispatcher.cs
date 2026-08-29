using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Features.v1.Preferences;
using FSH.Modules.Notifications.Templating;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.Channels;

/// <summary>Renders one template and fans it out across the enabled <see cref="INotificationChannel"/>s.</summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationRequest request, CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class NotificationDispatcher(
    IEnumerable<INotificationChannel> channels,
    INotificationTemplateRenderer templateRenderer,
    INotificationPreferenceService preferences,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    public async Task DispatchAsync(NotificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RecipientUserId);

        if (request.ExpectedTenantId is not null)
        {
            var ambient = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
            if (!string.Equals(ambient, request.ExpectedTenantId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Tenant context mismatch dispatching '{request.TemplateKey}': ambient " +
                    $"'{ambient ?? "(none)"}' != expected '{request.ExpectedTenantId}'. The publisher must " +
                    "establish the tenant's Finbuckle context before publishing (see eventing rules).");
            }
        }

        var effectiveChannels = request.PreferenceUserId is { } prefUser
            ? await preferences.EffectiveChannelsAsync(prefUser, request.TemplateKey, request.Channels, ct).ConfigureAwait(false)
            : request.Channels;

        if (effectiveChannels == NotificationChannelKind.None)
        {
            return;
        }

        var content = templateRenderer.Render(request.TemplateKey, request.Tokens);
        var delivery = new NotificationDelivery(
            RecipientUserId: request.RecipientUserId,
            RecipientEmail: request.RecipientEmail,
            Type: request.TemplateKey,
            Source: request.Source,
            Content: content,
            Metadata: request.Metadata);

        foreach (var channel in channels)
        {
            if (!effectiveChannels.HasFlag(channel.Kind))
            {
                continue;
            }

            await channel.SendAsync(delivery, ct).ConfigureAwait(false);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Dispatched {Type} to {Channels} for user {UserId}",
                request.TemplateKey, request.Channels, request.RecipientUserId);
        }
    }
}
