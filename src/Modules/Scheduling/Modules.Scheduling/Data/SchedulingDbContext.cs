using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Scheduling.Data;

public sealed class SchedulingDbContext : BaseDbContext
{
    public const string Schema = "scheduling";

    public SchedulingDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<SchedulingDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment)
    {
    }

    // Domain DbSets (ScheduleTemplate/Session/Attendance/Room/NonWorkingDay) land with the
    // migration in step 2 of the implementation plan — see
    // docs/04 Задачи/Задачи · Новые модули.md → Scheduling.

    // Required by AddEventingForDbContext<SchedulingDbContext>() from the very first migration —
    // added straight away (not patched in later) per the lesson learned from People, see
    // docs/04 Задачи/Задачи · Новые модули.md → People → the Outbox/Inbox bug note.
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration(Schema));

        // base.OnModelCreating runs LAST so tenant + soft-delete query filters layer on top of the
        // fully-configured model (Outbox/Inbox are IGlobalEntity, so they opt out).
        base.OnModelCreating(modelBuilder);
    }
}
