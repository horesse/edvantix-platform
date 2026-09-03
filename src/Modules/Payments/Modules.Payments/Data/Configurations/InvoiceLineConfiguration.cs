using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Payments.Data.Configurations;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("InvoiceLines");
        builder.HasKey(x => x.Id);

        // Domain assigns the key (Guid.CreateVersion7 in InvoiceLine.Create). Leaving it
        // store-generated makes EF classify a line added through an already-tracked invoice
        // (StudentInvoice.ReplaceLines during an update / draft refresh) as an existing row —
        // an UPDATE that affects 0 rows and throws DbUpdateConcurrencyException (EDX-020).
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Description).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Quantity).HasPrecision(18, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => x.InvoiceId);

        builder.Ignore(x => x.DomainEvents);
    }
}
