using FSH.Modules.People.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.People.Data.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Students");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.MiddleName).HasMaxLength(128);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.UserId).HasMaxLength(64);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Source).HasMaxLength(128);
        builder.Property(x => x.ManagerUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // A user (Identity account) can be linked to at most one student — tenant isolation
        // already scopes this filtered-unique index per-tenant via the shadow TenantId column.
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.ManagerUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsDeleted);

        // Child collections: not auto-included — SearchStudents lists shouldn't drag in every
        // guardian link and note. GetStudentGuardiansQuery/GetStudentNotesQuery Include explicitly.
        builder.HasMany(x => x.GuardianLinks)
            .WithOne()
            .HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DisplayName);
        builder.Ignore(x => x.DomainEvents);
    }
}
