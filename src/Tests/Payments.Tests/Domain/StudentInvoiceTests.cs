using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;

namespace Payments.Tests.Domain;

public sealed class StudentInvoiceTests
{
    private static StudentInvoice CreateDraft(DateOnly? periodFrom = null, DateOnly? periodTo = null) => StudentInvoice.Create(
        studentId: Guid.NewGuid(),
        payerGuardianId: null,
        studyGroupId: Guid.NewGuid(),
        periodFrom: periodFrom ?? new DateOnly(2026, 9, 1),
        periodTo: periodTo ?? new DateOnly(2026, 9, 30),
        dueDate: new DateOnly(2026, 9, 10),
        currency: "usd",
        comment: null);

    #region Create / lines

    [Fact]
    public void Create_Should_StartAsDraft_With_ZeroTotals()
    {
        var invoice = CreateDraft();

        invoice.Status.ShouldBe(InvoiceStatus.Draft);
        invoice.Total.ShouldBe(0m);
        invoice.PaidAmount.ShouldBe(0m);
        invoice.Currency.ShouldBe("USD");
    }

    [Fact]
    public void ReplaceLines_Should_SetTotal_To_SumOfLineAmounts()
    {
        var invoice = CreateDraft();

        invoice.ReplaceLines(
        [
            ("Tuition", null, 2m, 100m),
            ("Books", null, 1m, 50.5m),
        ]);

        // 2*100 + 1*50.5 = 250.5
        invoice.Total.ShouldBe(250.5m);
        invoice.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public void ReplaceLines_Should_RoundAmount_AwayFromZero()
    {
        var invoice = CreateDraft();

        // 3 * 33.335 = 100.005 -> rounds to 100.01 (AwayFromZero at 2dp)
        invoice.ReplaceLines([("Tuition", null, 3m, 33.335m)]);

        invoice.Lines[0].Amount.ShouldBe(100.01m);
        invoice.Total.ShouldBe(100.01m);
    }

    [Fact]
    public void ReplaceLines_Should_Throw_When_NotDraft()
    {
        var invoice = CreateDraft();
        invoice.ReplaceLines([("Tuition", null, 1m, 100m)]);
        invoice.Issue(new DateOnly(2026, 9, 1));

        Should.Throw<CustomException>(() => invoice.ReplaceLines([("Extra", null, 1m, 10m)]));
    }

    #endregion

    #region Issue

    [Fact]
    public void Issue_Should_Throw_When_NoLines()
    {
        var invoice = CreateDraft();

        Should.Throw<CustomException>(() => invoice.Issue(new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void Issue_Should_SetStatusIssued_And_IssuedOn()
    {
        var invoice = CreateDraft();
        invoice.ReplaceLines([("Tuition", null, 1m, 100m)]);

        invoice.Issue(new DateOnly(2026, 9, 1));

        invoice.Status.ShouldBe(InvoiceStatus.Issued);
        invoice.IssuedOn.ShouldBe(new DateOnly(2026, 9, 1));
    }

    #endregion

    #region Payments — partial, full, overpayment, reversal

    private static StudentInvoice CreateIssued(decimal total = 100m)
    {
        var invoice = CreateDraft();
        invoice.ReplaceLines([("Tuition", null, 1m, total)]);
        invoice.Issue(new DateOnly(2026, 9, 1));
        return invoice;
    }

    [Fact]
    public void ConfirmPayment_Should_SetPartiallyPaid_When_LessThanTotal()
    {
        var invoice = CreateIssued(100m);

        invoice.ConfirmPayment(40m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);

        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
        invoice.PaidAmount.ShouldBe(40m);
    }

    [Fact]
    public void ConfirmPayment_Should_SetPaid_When_EqualToTotal()
    {
        var invoice = CreateIssued(100m);

        invoice.ConfirmPayment(100m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.PaidAmount.ShouldBe(100m);
    }

    [Fact]
    public void ConfirmPayment_Should_AllowOverpayment_And_StaySetPaid()
    {
        var invoice = CreateIssued(100m);

        invoice.ConfirmPayment(130m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.PaidAmount.ShouldBe(130m);
        // The surplus (30) is a read-side advance-balance concern, not rejected or clamped here.
    }

    [Fact]
    public void ConfirmPayment_Should_SumMultiplePayments()
    {
        var invoice = CreateIssued(100m);

        invoice.ConfirmPayment(30m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);
        invoice.ConfirmPayment(40m, new DateOnly(2026, 9, 10), PaymentMethod.BankTransfer, null, null, "user-1", null);

        invoice.PaidAmount.ShouldBe(70m);
        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void ConfirmPayment_Should_Throw_When_InvoiceIsDraft()
    {
        var invoice = CreateDraft();
        invoice.ReplaceLines([("Tuition", null, 1m, 100m)]);

        Should.Throw<CustomException>(() =>
            invoice.ConfirmPayment(50m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null));
    }

    [Fact]
    public void ReversePayment_Should_SubtractAmount_And_RevertStatus()
    {
        var invoice = CreateIssued(100m);
        var payment = invoice.ConfirmPayment(100m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);
        invoice.Status.ShouldBe(InvoiceStatus.Paid);

        invoice.ReversePayment(payment.Id, "admin-1", "mistake");

        invoice.PaidAmount.ShouldBe(0m);
        invoice.Status.ShouldBe(InvoiceStatus.Issued);
    }

    [Fact]
    public void ReversePayment_Should_RevertToPartiallyPaid_When_OtherPaymentsRemain()
    {
        var invoice = CreateIssued(100m);
        invoice.ConfirmPayment(30m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);
        var second = invoice.ConfirmPayment(40m, new DateOnly(2026, 9, 10), PaymentMethod.Cash, null, null, "user-1", null);

        invoice.ReversePayment(second.Id, "admin-1", null);

        invoice.PaidAmount.ShouldBe(30m);
        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void ReversePayment_Should_Throw_When_AlreadyReversed()
    {
        var invoice = CreateIssued(100m);
        var payment = invoice.ConfirmPayment(100m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);
        invoice.ReversePayment(payment.Id, "admin-1", null);

        Should.Throw<CustomException>(() => invoice.ReversePayment(payment.Id, "admin-1", null));
    }

    [Fact]
    public void ReversePayment_Should_Throw_When_ReversingAReversal()
    {
        var invoice = CreateIssued(100m);
        var payment = invoice.ConfirmPayment(100m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);
        var reversal = invoice.ReversePayment(payment.Id, "admin-1", null);

        Should.Throw<CustomException>(() => invoice.ReversePayment(reversal.Id, "admin-1", null));
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_Should_Succeed_When_NoPayments()
    {
        var invoice = CreateIssued(100m);

        invoice.Cancel("no longer needed");

        invoice.Status.ShouldBe(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Throw_When_HasPayments()
    {
        var invoice = CreateIssued(100m);
        invoice.ConfirmPayment(10m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);

        Should.Throw<CustomException>(() => invoice.Cancel(null));
    }

    [Fact]
    public void Cancel_Should_Throw_When_Draft()
    {
        var invoice = CreateDraft();

        Should.Throw<CustomException>(() => invoice.Cancel(null));
    }

    #endregion

    #region Overdue

    [Fact]
    public void IsOverdue_Should_BeTrue_When_IssuedAndPastDueDate()
    {
        var invoice = CreateIssued(100m);

        invoice.IsOverdue(new DateOnly(2026, 9, 30)).ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_Should_BeFalse_When_Paid()
    {
        var invoice = CreateIssued(100m);
        invoice.ConfirmPayment(100m, new DateOnly(2026, 9, 5), PaymentMethod.Cash, null, null, "user-1", null);

        invoice.IsOverdue(new DateOnly(2026, 9, 30)).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_Should_BeFalse_When_BeforeDueDate()
    {
        var invoice = CreateIssued(100m);

        invoice.IsOverdue(new DateOnly(2026, 9, 5)).ShouldBeFalse();
    }

    #endregion
}
