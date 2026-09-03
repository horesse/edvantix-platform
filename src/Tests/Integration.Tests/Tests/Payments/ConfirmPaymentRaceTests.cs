using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Payments.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Payments;

/// <summary>
/// EDX-020 — recording and reversing a payment over HTTP must succeed on the first try. The bug:
/// <c>PaymentConfirmation</c>/<c>InvoiceLine</c> keys were left store-generated while the domain
/// assigns them (<c>Guid.CreateVersion7</c>), so a child added through the already-tracked
/// <c>StudentInvoice</c> aggregate was classified <c>Modified</c>, not <c>Added</c> — an UPDATE that
/// affected 0 rows, a <c>DbUpdateConcurrencyException</c>, and a permanent 409 from
/// <c>InvoiceWrite.WithConcurrencyRetryAsync</c>. Nothing external races the row.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ConfirmPaymentRaceTests
{
    private readonly AuthHelper _auth;

    public ConfirmPaymentRaceTests(FshWebApplicationFactory factory) => _auth = new AuthHelper(factory);

    [Fact]
    public async Task Payment_Is_Recorded_And_Reversed_Over_Http_Without_A_Spurious_Conflict()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var create = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices",
            new
            {
                studentId = Guid.NewGuid(),
                payerGuardianId = (Guid?)null,
                studyGroupId = (Guid?)null,
                periodFrom = today.AddMonths(-1),
                periodTo = today,
                dueDate = today.AddDays(7),
                currency = "USD",
                comment = (string?)null,
                lines = new[] { new { description = "Обучение", tariffId = (Guid?)null, quantity = 1m, unitPrice = 100m } },
            });
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await create.Content.ReadAsStringAsync());
        var invoiceId = await create.DeserializeAsync<Guid>();

        using var issue = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}/issue",
            new { issuedOn = today.AddMonths(-1) });
        issue.StatusCode.ShouldBe(HttpStatusCode.NoContent, await issue.Content.ReadAsStringAsync());

        // Pay in full — this is the call that used to 409 every time.
        using var pay = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}/payments",
            new { amount = 100m, paidOn = today, method = "Cash", reference = (string?)null, proofFileId = (Guid?)null, note = (string?)null });
        pay.StatusCode.ShouldBe(HttpStatusCode.OK, await pay.Content.ReadAsStringAsync());
        var paymentId = await pay.DeserializeAsync<Guid>();

        var afterPay = await (await client.GetAsync($"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}"))
            .DeserializeAsync<StudentInvoiceDetailDto>();
        afterPay.Status.ShouldBe(InvoiceStatus.Paid);
        afterPay.PaidAmount.ShouldBe(100m);
        afterPay.Payments.Count.ShouldBe(1);

        // Reversal walks the same add-child-through-tracked-aggregate path.
        using var reverse = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/payments/{paymentId}/reverse",
            new { note = "test reversal" });
        reverse.StatusCode.ShouldBe(HttpStatusCode.OK, await reverse.Content.ReadAsStringAsync());

        var afterReverse = await (await client.GetAsync($"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}"))
            .DeserializeAsync<StudentInvoiceDetailDto>();
        afterReverse.Status.ShouldBe(InvoiceStatus.Issued);
        afterReverse.PaidAmount.ShouldBe(0m);
        afterReverse.Payments.Count.ShouldBe(2);
    }
}
