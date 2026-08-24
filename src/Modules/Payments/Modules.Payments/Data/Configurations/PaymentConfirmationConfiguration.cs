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
