using FSH.Modules.Payments.Domain;

namespace Payments.Tests.Domain;

public sealed class InvoiceNumberFormatTests
{
    private static readonly DateOnly SampleDate = new(2026, 3, 7);

    [Theory]
    [InlineData("{YYYY}-{NNNN}", 1, "2026-0001")]
    [InlineData("{YYYY}-{NNNN}", 42, "2026-0042")]
    [InlineData("{YYYY}-{NNNN}", 12345, "2026-12345")] // wider than the mask → printed in full, never truncated
    [InlineData("INV-{YY}{MM}-{NNN}", 8, "INV-2603-008")]
    [InlineData("{N}", 7, "7")]
    [InlineData("ACME/{YYYY}/{NNNNNN}", 250, "ACME/2026/000250")]
    public void Render_Should_Substitute_Placeholders(string template, long sequence, string expected)
    {
        InvoiceNumberFormat.Render(template, sequence, SampleDate).ShouldBe(expected);
    }

    [Fact]
    public void Render_Should_AppendCounter_When_TemplateHasNoCounterToken()
    {
        // Defensive: a stored template with no {N…} would otherwise render identically for every
        // invoice and collide on the unique index.
        InvoiceNumberFormat.Render("{YYYY}", 5, SampleDate).ShouldBe("2026-0005");
    }

    [Theory]
    [InlineData("{YYYY}-{NNNN}", true)]
    [InlineData("{YY}/{NNNN}", true)]
    [InlineData("INV-{NNNN}", false)]
    [InlineData("{MM}-{NNNN}", false)] // month alone is not a yearly reset
    public void IsYearScoped_Should_Track_YearPlaceholders(string template, bool expected)
    {
        InvoiceNumberFormat.IsYearScoped(template).ShouldBe(expected);
    }

    [Theory]
    [InlineData("{YYYY}-{NNNN}")]
    [InlineData("INV-{NNNN}")]
    [InlineData("{YY}{MM}{NNNNNN}")]
    [InlineData("{N}")]
    public void IsValid_Should_Accept_WellFormedTemplates(string template)
    {
        InvoiceNumberFormat.IsValid(template).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{YYYY}-0001")]           // no counter token
    [InlineData("{YYYY}-{XXXX}")]         // unknown placeholder
    [InlineData("{YYYY}-{NNNN")]          // unbalanced brace
    [InlineData("{NNNNNNNNNNN}")]         // 11 N's — outside 1..10
    public void IsValid_Should_Reject_MalformedTemplates(string? template)
    {
        InvoiceNumberFormat.IsValid(template).ShouldBeFalse();
    }
}
