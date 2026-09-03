using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Payments.Data.Configurations;

public sealed class PaymentConfirmationConfiguration : IEntityTypeConfiguration<PaymentConfirmation>
{
    public void Configure(EntityTypeBuilder<PaymentConfirmation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PaymentConfirmations");
        builder.HasKey(x => x.Id);

        // Domain assigns the key (Guid.CreateVersion7 in PaymentConfirmation.Create). Without this,
        // EF treats the property as store-generated and — when a new payment is discovered through
        // the already-tracked StudentInvoice aggregate's Payments collection during DetectChanges —
        // classifies the populated key as an *existing* row (Modified), emitting an UPDATE that
        // affects 0 rows and throws DbUpdateConcurrencyException (EDX-020).
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Method).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Reference).HasMaxLength(128);
        builder.Property(x => x.ConfirmedByUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.ReversesId);

        builder.Ignore(x => x.DomainEvents);
    }
}
