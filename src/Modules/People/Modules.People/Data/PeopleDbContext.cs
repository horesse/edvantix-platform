using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.People.Data;

public sealed class PeopleDbContext : BaseDbContext
{
    public const string Schema = "people";

    public PeopleDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PeopleDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<StudentNote> StudentNotes => Set<StudentNote>();

    // Required by AddEventingForDbContext<PeopleDbContext>() — EfCoreOutboxStore/EfCoreInboxStore
    // do Set<OutboxMessage>()/Set<InboxMessage>() against THIS context, so the model must include
    // them explicitly (not automatic — mirrors IdentityDbContext, the only other module that
    // currently publishes events transactionally).
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PeopleDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration(Schema));

        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply sees fully-configured
        // entities before it layers on tenant + soft-delete query filters (Outbox/Inbox are
        // IGlobalEntity, so they opt out — same as in IdentityDbContext).
        base.OnModelCreating(modelBuilder);
    }
}
