using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Persistence;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Covers the "история этого ученика" filter: <c>GET /audits/by-entity/{entityName}/{entityId}</c>
/// and the <c>EntityName</c>/<c>EntityKey</c> filters on <c>GetAuditsQuery</c>. Seeds
/// <see cref="AuditEventType.EntityChange"/> rows with a known <see cref="EntityChangeEventPayload"/>
/// and drives the real endpoint.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AuditEntityHistoryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AuditEntityHistoryTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ByEntity_Should_ReturnOnlyRows_For_TheGivenEntity()
    {
        var studentId = Guid.NewGuid();
        var targetCorrelation = $"ent-{Guid.NewGuid():N}";
        var otherStudentCorrelation = $"ent-{Guid.NewGuid():N}";
        var otherTypeCorrelation = $"ent-{Guid.NewGuid():N}";

        await SeedEntityChangeAsync("Student", $"Id:{studentId}", targetCorrelation);
        await SeedEntityChangeAsync("Student", $"Id:{Guid.NewGuid()}", otherStudentCorrelation);
        await SeedEntityChangeAsync("Invoice", $"Id:{studentId}", otherTypeCorrelation);

        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/by-entity/Student/{studentId}?pageNumber=1&pageSize=100");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paged = await ReadPagedAsync(response);

        paged.Items.ShouldContain(r => r.CorrelationId == targetCorrelation);
        paged.Items.ShouldNotContain(r => r.CorrelationId == otherStudentCorrelation);
        paged.Items.ShouldNotContain(r => r.CorrelationId == otherTypeCorrelation);
    }

    [Fact]
    public async Task GetAudits_Should_Return400_When_EntityKey_Without_EntityName()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=10&entityKey=Id:{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task SeedEntityChangeAsync(string entityName, string key, string correlationId)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var store = sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var rootTenant = await store.GetAsync(MultitenancyConstants.Root.Id);
        rootTenant.ShouldNotBeNull();

        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(rootTenant);

        var payload = new SystemTextJsonAuditSerializer().SerializePayload(new EntityChangeEventPayload(
            DbContext: "PeopleDbContext",
            Schema: "people",
            Table: entityName + "s",
            EntityName: entityName,
            Key: key,
            Operation: EntityOperation.Update,
            Changes: [new PropertyChange("Status", "string", "Active", "Archived", false)],
            TransactionId: null));

        var db = sp.GetRequiredService<AuditDbContext>();
        db.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow,
            EventType = (int)AuditEventType.EntityChange,
            Severity = (byte)AuditSeverity.Information,
            TenantId = MultitenancyConstants.Root.Id,
            UserId = "hist-user",
            UserName = "hist-user",
            CorrelationId = correlationId,
            Source = "hist",
            Tags = 0,
            PayloadJson = payload,
        });

        await db.SaveChangesAsync();
    }

    private static async Task<PagedResult<AuditSummaryDto>> ReadPagedAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<AuditSummaryDto>>(json, JsonOptions)
            ?? new PagedResult<AuditSummaryDto>();
    }
}
