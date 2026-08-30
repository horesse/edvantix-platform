using System.Text.Json;
using System.Text.Json.Nodes;
using FSH.Modules.Webhooks.Services;

namespace Webhooks.Tests.Services;

public sealed class WebhookEnvelopeBuilderTests
{
    [Fact]
    public void Build_From_EventJson_Should_Wrap_In_Envelope_And_Strip_Transport_Keys()
    {
        var eventId = Guid.CreateVersion7();
        var occurredAt = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        var eventJson = """
            {
              "id": "00000000-0000-0000-0000-000000000001",
              "occurredOnUtc": "2026-08-30T10:00:00Z",
              "tenantId": "acme",
              "correlationId": "abc",
              "source": "StudyGroups",
              "studyGroupId": "11111111-1111-1111-1111-111111111111",
              "studentId": "22222222-2222-2222-2222-222222222222"
            }
            """;

        var result = WebhookEnvelopeBuilder.Build(eventId, "StudentEnrolledIntegrationEvent", "acme", occurredAt, eventJson);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("id").GetGuid().ShouldBe(eventId);
        root.GetProperty("type").GetString().ShouldBe("StudentEnrolledIntegrationEvent");
        root.GetProperty("tenantId").GetString().ShouldBe("acme");
        root.GetProperty("occurredAt").GetDateTime().ShouldBe(occurredAt);

        var data = root.GetProperty("data");
        data.TryGetProperty("id", out _).ShouldBeFalse();
        data.TryGetProperty("occurredOnUtc", out _).ShouldBeFalse();
        data.TryGetProperty("tenantId", out _).ShouldBeFalse();
        data.TryGetProperty("correlationId", out _).ShouldBeFalse();
        data.TryGetProperty("source", out _).ShouldBeFalse();
        data.GetProperty("studyGroupId").GetString().ShouldBe("11111111-1111-1111-1111-111111111111");
        data.GetProperty("studentId").GetString().ShouldBe("22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void Build_From_DataNode_Should_Produce_Same_Envelope_Shape()
    {
        var data = new JsonObject { ["message"] = "hi" };

        var result = WebhookEnvelopeBuilder.Build(Guid.CreateVersion7(), "webhook.test", "acme", DateTime.UtcNow, data);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("webhook.test");
        doc.RootElement.GetProperty("data").GetProperty("message").GetString().ShouldBe("hi");
    }

    [Fact]
    public void Build_Should_Reject_Blank_EventType() =>
        Should.Throw<ArgumentException>(() =>
            WebhookEnvelopeBuilder.Build(Guid.NewGuid(), " ", "acme", DateTime.UtcNow, "{}"));
}
