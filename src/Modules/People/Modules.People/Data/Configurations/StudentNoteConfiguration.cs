using FSH.Modules.People.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.People.Data.Configurations;

public sealed class StudentNoteConfiguration : IEntityTypeConfiguration<StudentNote>
{
    public void Configure(EntityTypeBuilder<StudentNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StudentNotes");
        builder.HasKey(x => x.Id);

        // Same app-assigned-Guid-via-nav-collection footgun as StudentGuardian/ProductImage.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.StudentId).IsRequired();
        builder.Property(x => x.Text).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.AuthorUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.IsDeleted);
    }
}
