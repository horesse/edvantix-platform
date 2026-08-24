using System.Globalization;
using FSH.Modules.Payments.Contracts.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSH.Modules.Payments.Services;

/// <summary>
/// QuestPDF-based invoice renderer — same tool as Billing's <c>InvoicePdfRenderer</c> (Community
/// license, free under $1M USD/year revenue), independent implementation (see
/// <see cref="IInvoicePdfRenderer"/> remarks on why the interface isn't literally shared).
/// </summary>
public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    static InvoicePdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(StudentInvoiceDetailDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text("INVOICE").FontSize(22).Bold();
                    col.Item().Text(invoice.Number).FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Student").SemiBold();
                            c.Item().Text(invoice.StudentId.ToString());
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Status: {invoice.Status}").SemiBold();
                            c.Item().Text($"Period: {invoice.PeriodFrom:yyyy-MM-dd} → {invoice.PeriodTo:yyyy-MM-dd}");
                        });
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Issued: {FormatDate(invoice.IssuedOn)}");
                        row.RelativeItem().AlignRight().Text($"Due: {invoice.DueDate:yyyy-MM-dd}");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(5);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").SemiBold();
                            header.Cell().AlignRight().Text("Qty").SemiBold();
                            header.Cell().AlignRight().Text("Unit Price").SemiBold();
                            header.Cell().AlignRight().Text("Amount").SemiBold();
                        });

                        foreach (var line in invoice.Lines)
                        {
                            table.Cell().Text(line.Description);
                            table.Cell().AlignRight().Text(line.Quantity.ToString("0.##", Culture));
                            table.Cell().AlignRight().Text(line.UnitPrice.ToString("0.00", Culture));
                            table.Cell().AlignRight().Text(line.Amount.ToString("0.00", Culture));
                        }
                    });

                    col.Item().AlignRight()
                        .Text($"Total: {invoice.Total.ToString("0.00", Culture)} {invoice.Currency}")
                        .FontSize(12).Bold();
                    col.Item().AlignRight()
                        .Text($"Paid: {invoice.PaidAmount.ToString("0.00", Culture)} {invoice.Currency}")
                        .FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(invoice.Comment))
                    {
                        col.Item().PaddingTop(10).Text($"Comment: {invoice.Comment}").FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"Generated {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", Culture)}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    private static string FormatDate(DateOnly? value) =>
        value is null ? "—" : value.Value.ToString("yyyy-MM-dd", Culture);
}
