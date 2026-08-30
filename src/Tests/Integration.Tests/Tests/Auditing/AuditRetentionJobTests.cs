using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Persistence;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Covers <see cref="AuditRetentionJob"/> against real Postgres: it purges rows older than the
/// per-event-type window, keeps fresh ones, and refuses to run on a non-positive window (which
/// would translate to a future cutoff and wipe the table).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AuditRetentionJobTests
{
    private readonly FshWebApplicationFactory _factory;

    public AuditRetentionJobTests(FshWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Run_Should_Purge_Rows_Older_Than_The_Window_And_Keep_Fresh_Ones()
    {
        var oldCorrelation = $"ret-old-{Guid.NewGuid():N}";
        var freshCorrelation = $"ret-fresh-{Guid.NewGuid():N}";

        await SeedEntityChangeAsync(oldCorrelation, DateTime.UtcNow.AddDays(-400));
        await SeedEntityChangeAsync(freshCorrelation, DateTime.UtcNow.AddDays(-1));

        await RunJobAsync(new AuditRetentionOptions { Enabled = true, EntityChangeRetentionDays = 365 });

        await AssertPresenceAsync(oldCorrelation, expected: false);
        await AssertPresenceAsync(freshCorrelation, expected: true);
    }

    [Fact]
    public async Task Run_Should_NoOp_When_A_Window_Is_Not_Positive()
    {
        var correlation = $"ret-guard-{Guid.NewGuid():N}";
        await SeedEntityChangeAsync(correlation, DateTime.UtcNow.AddDays(-400));

        await RunJobAsync(new AuditRetentionOptions { Enabled = true, EntityChangeRetentionDays = 0 });

        await AssertPresenceAsync(correlation, expected: true);
    }

    private async Task RunJobAsync(AuditRetentionOptions options)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var rootTenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(MultitenancyConstants.Root.Id);
        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(rootTenant!);

        var job = new AuditRetentionJob(
            sp.GetRequiredService<AuditDbContext>(),
            options,
            TimeProvider.System,
            NullLogger<AuditRetentionJob>.Instance);

        await job.RunAsync(CancellationToken.None);
    }

    private async Task AssertPresenceAsync(string correlationId, bool expected)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var rootTenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(MultitenancyConstants.Root.Id);
        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(rootTenant!);

        var db = sp.GetRequiredService<AuditDbContext>();
        (await db.AuditRecords.AnyAsync(r => r.CorrelationId == correlationId)).ShouldBe(expected);
    }

    private async Task SeedEntityChangeAsync(string correlationId, DateTime occurredAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var rootTenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(MultitenancyConstants.Root.Id);
        rootTenant.ShouldNotBeNull();
        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(rootTenant);

        var payload = new SystemTextJsonAuditSerializer().SerializePayload(new EntityChangeEventPayload(
            DbContext: "PeopleDbContext",
            Schema: "people",
            Table: "Students",
            EntityName: "Student",
            Key: $"Id:{Guid.NewGuid()}",
            Operation: EntityOperation.Update,
            Changes: [new PropertyChange("Status", "string", "Active", "Archived", false)],
            TransactionId: null));

        var db = sp.GetRequiredService<AuditDbContext>();
        db.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = occurredAtUtc,
            EventType = (int)AuditEventType.EntityChange,
            Severity = (byte)AuditSeverity.Information,
            TenantId = MultitenancyConstants.Root.Id,
            UserId = "ret-user",
            UserName = "ret-user",
            CorrelationId = correlationId,
            Source = "ret",
            Tags = 0,
            PayloadJson = payload,
        });

        await db.SaveChangesAsync();
    }
}
