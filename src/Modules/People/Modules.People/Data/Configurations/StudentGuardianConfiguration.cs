using FSH.Modules.People.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.People.Data.Configurations;

public sealed class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StudentGuardians");
        builder.HasKey(x => x.Id);

        // Id is app-assigned (Guid.CreateVersion7). Without ValueGeneratedNever, EF treats the
        // non-default Guid reached via the tracked Student's nav collection as persisted →
        // UPDATE-0-rows → concurrency exception (same footgun as Catalog.ProductImage).
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.StudentId).IsRequired();
        builder.Property(x => x.GuardianId).IsRequired();
        builder.Property(x => x.Relation).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IsPrimaryPayer).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // A guardian can be re-linked to the same student after a previous link was removed
        // (soft-deleted) — the filter keeps the unique constraint scoped to the live link only.
        builder.HasIndex(x => new { x.StudentId, x.GuardianId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.GuardianId);
        builder.HasIndex(x => x.IsDeleted);

        // No partial UNIQUE on (StudentId) WHERE IsPrimaryPayer: same reasoning as Catalog's
        // single-thumbnail — a Postgres partial unique index can't be made deferrable enough
        // for the demote-then-promote sequence in one transaction. Student.SetPrimaryPayer
        // enforces "exactly one" in the aggregate instead.
    }
}
