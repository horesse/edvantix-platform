using System.Text.Json.Nodes;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Webhooks.Contracts.v1.TestWebhookSubscription;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Webhooks.Features.v1.TestWebhookSubscription;

public sealed class TestWebhookSubscriptionCommandHandler(
    WebhookDbContext dbContext,
    IWebhookDeliveryService deliveryService,
    IWebhookSecretProtector secretProtector,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor) : ICommandHandler<TestWebhookSubscriptionCommand, bool>
{
    public async ValueTask<bool> Handle(TestWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Webhook subscription {command.Id} not found.");

        var tenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id ?? string.Empty;
        var data = new JsonObject { ["message"] = "This is a test webhook delivery." };
        var testPayload = WebhookEnvelopeBuilder.Build(
            Guid.CreateVersion7(), "webhook.test", tenantId,
            TimeProvider.System.GetUtcNow().UtcDateTime, data);

        await deliveryService.DeliverAsync(
            subscription.Id,
            subscription.Url,
            secretProtector.Unprotect(subscription.SecretHash),
            "webhook.test",
            testPayload,
            cancellationToken).ConfigureAwait(false);

        return true;
    }
}
