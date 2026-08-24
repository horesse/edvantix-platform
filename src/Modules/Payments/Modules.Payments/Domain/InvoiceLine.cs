using FSH.Framework.Core.Domain;

namespace FSH.Modules.Payments.Domain;

/// <summary>
/// One priced line on a <see cref="StudentInvoice"/> — tuition for a period, a package, a one-time
/// charge. Owned by the invoice, mutated only through <see cref="StudentInvoice.ReplaceLines"/> and
/// only while the invoice is <c>Draft</c> (see docs/02 Модули/Payments.md → «Инварианты»: "после
/// Issued строки неизменяемы").
/// </summary>
public sealed class InvoiceLine : BaseEntity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public Guid? TariffId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }

    private InvoiceLine() { }

    internal static InvoiceLine Create(Guid invoiceId, string description, Guid? tariffId, decimal quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");
        }

        return new InvoiceLine
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = invoiceId,
            Description = description.Trim(),
            TariffId = tariffId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero),
        };
    }
}
