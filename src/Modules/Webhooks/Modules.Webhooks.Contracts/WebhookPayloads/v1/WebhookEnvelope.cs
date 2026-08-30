using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FSH.Modules.Webhooks.Contracts.WebhookPayloads.v1;

/// <summary>
/// The stable outer contract for every webhook body Edvantix sends. Integrators bind to
/// <b>this</b> shape, not to the internal integration-event record: the module owns it, so a
/// refactor on the publishing side cannot silently reshape the wire body.
///
/// <para>
/// <see cref="Data"/> carries the event-specific fields with the transport/envelope noise
/// (<c>id</c>, <c>occurredOnUtc</c>, <c>tenantId</c>, <c>correlationId</c>, <c>source</c>) stripped
/// out — those are promoted to, or dropped in favour of, the typed envelope fields here. The
/// per-event <c>data</c> field lists live in <c>docs/02 Модули/Webhooks.md</c> → «Тело вебхука».
/// </para>
///
/// <para>Versioned by namespace (<c>WebhookPayloads.v1</c>): a breaking change ships a <c>v2</c>
/// envelope while <c>v1</c> keeps flowing until integrators migrate.</para>
/// </summary>
public sealed record WebhookEnvelope(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("data")] JsonNode? Data);
