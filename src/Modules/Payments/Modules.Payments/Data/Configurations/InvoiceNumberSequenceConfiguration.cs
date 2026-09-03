using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Payments.Data.Configurations;

public sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequence>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequence> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("InvoiceNumberSequences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Scope).IsRequired().HasMaxLength(16);
        builder.Property(x => x.NextValue).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // The ON CONFLICT target for the atomic block-reservation upsert (see InvoiceNumberGenerator).
        builder.HasIndex(x => new { x.TenantId, x.Scope }).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
