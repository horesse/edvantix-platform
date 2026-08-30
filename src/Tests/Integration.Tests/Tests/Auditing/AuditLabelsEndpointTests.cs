using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FSH.Modules.Auditing.Contracts.v1.GetAuditLabels;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Covers <c>GET /api/v1/audits/entity-labels</c> — the friendly-label lookup the history UI uses
/// so it renders "Ученик" / "Статус" instead of raw CLR names.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AuditLabelsEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AuditLabelsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task EntityLabels_Should_Return_Entity_And_Field_Maps()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync("/api/v1/audits/entity-labels");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var labels = await response.Content.ReadFromJsonAsync<AuditLabels>(JsonOptions);
        labels.ShouldNotBeNull();
        labels!.Entities["Student"].ShouldBe("Ученик");
        labels.Fields["Status"].ShouldBe("Статус");
    }

    [Fact]
    public async Task EntityLabels_Should_Require_Authentication()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/audits/entity-labels");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
