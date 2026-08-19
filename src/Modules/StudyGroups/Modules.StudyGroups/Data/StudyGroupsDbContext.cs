using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.StudyGroups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.StudyGroups.Data;

public sealed class StudyGroupsDbContext : BaseDbContext
{
    public const string Schema = "study_groups";

    public StudyGroupsDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<StudyGroupsDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment)
    {
    }

    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<GroupEnrollment> GroupEnrollments => Set<GroupEnrollment>();
    public DbSet<GroupTeacher> GroupTeachers => Set<GroupTeacher>();

    // Required by AddEventingForDbContext<StudyGroupsDbContext>() — added from the first migration
    // (unlike People, which had to patch this in with a second migration — see
    // docs/04 Задачи/Задачи · Новые модули.md → People → the Outbox/Inbox bug note).
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudyGroupsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration(Schema));

        // base.OnModelCreating runs LAST so tenant + soft-delete query filters layer on top of the
        // fully-configured model (Outbox/Inbox are IGlobalEntity, so they opt out).
        base.OnModelCreating(modelBuilder);
    }
}
