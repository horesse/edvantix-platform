using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Payments.Data;

public sealed class PaymentsDbContext : BaseDbContext
{
    public const string Schema = "payments";

    public PaymentsDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PaymentsDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment)
    {
    }

    // Domain DbSets (Tariff/StudentInvoice/InvoiceLine/PaymentConfirmation) are added in the next
    // step alongside the entities themselves and the InitialCreate migration — see
    // docs/04 Задачи/Задачи · Новые модули.md → Payments → шаг 3.

    // Required by AddEventingForDbContext<PaymentsDbContext>() — present from the first migration
    // (same as StudyGroups/Scheduling; see People's Outbox/Inbox bug note for why this matters).
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration(Schema));

        // base.OnModelCreating runs LAST so tenant + soft-delete query filters layer on top of the
        // fully-configured model (Outbox/Inbox are IGlobalEntity, so they opt out).
        base.OnModelCreating(modelBuilder);
    }
}
