using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Payments.Data.Configurations;

public sealed class StudentInvoiceConfiguration : IEntityTypeConfiguration<StudentInvoice>
{
    public void Configure(EntityTypeBuilder<StudentInvoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StudentInvoices");
        builder.HasKey(x => x.Id);

        // Client-assigned key (Guid.CreateVersion7 in StudentInvoice.Create) — keep EF from treating
        // a populated key as a store-generated one. See PaymentConfirmationConfiguration / EDX-020.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Number).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(8);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.Comment).HasMaxLength(2000);

        // Unique per tenant, same reasoning as StudyGroup.Code — physical-schema-per-tenant model
        // means a plain unique index is enough, no need to fold TenantId in.
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.StudyGroupId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DueDate);

        // Not auto-included — SearchStudentInvoices lists shouldn't drag in every line/payment row.
        // GetStudentInvoiceByIdQuery Includes explicitly.
        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
