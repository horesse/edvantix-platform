using System.Text.Json;
using System.Text.Json.Nodes;
using FSH.Modules.Webhooks.Contracts.WebhookPayloads.v1;

namespace FSH.Modules.Webhooks.Services;

/// <summary>
/// Wraps a serialized integration event into the public <see cref="WebhookEnvelope"/> shape that
/// actually goes on the wire. The event's own JSON is reused for the <c>data</c> object with the
/// transport keys removed, so the body an integrator sees is <c>{ id, type, occurredAt, tenantId,
/// data }</c> rather than a flat dump of the internal record.
/// </summary>
public static class WebhookEnvelopeBuilder
{
    // camelCase because JsonEventSerializer emits camelCase.
    private static readonly string[] TransportKeys =
        ["id", "occurredOnUtc", "tenantId", "correlationId", "source"];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Builds the envelope JSON for a real integration event. <paramref name="eventJson"/> is the
    /// output of <c>IEventSerializer.Serialize</c>.
    /// </summary>
    public static string Build(Guid eventId, string eventType, string tenantId, DateTime occurredAtUtc, string eventJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventJson);

        JsonNode? data = JsonNode.Parse(eventJson);
        if (data is JsonObject obj)
        {
            foreach (var key in TransportKeys)
            {
                obj.Remove(key);
            }
        }

        return Serialize(new WebhookEnvelope(eventId, eventType, occurredAtUtc, tenantId, data));
    }

    /// <summary>Builds an envelope around an already-shaped <c>data</c> node (used for test deliveries).</summary>
    public static string Build(Guid eventId, string eventType, string tenantId, DateTime occurredAtUtc, JsonNode? data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(tenantId);

        return Serialize(new WebhookEnvelope(eventId, eventType, occurredAtUtc, tenantId, data));
    }

    private static string Serialize(WebhookEnvelope envelope) => JsonSerializer.Serialize(envelope, Options);
}
