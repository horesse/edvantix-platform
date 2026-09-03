using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentBalance;
using FSH.Modules.Scheduling.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;

namespace Payments.Tests.Features;

/// <summary>Arithmetic for the <c>PerPackage</c> remaining-sessions projection (see docs/02
/// Модули/Payments.md → «Баланс» and «Модель начисления» — "остаток пакета — проекция"), by the
/// same reasoning/pattern as <c>TariffAccrualServiceTests</c> for the accrual side.</summary>
public sealed class GetStudentBalanceQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 9, 20);

    [Fact]
    public async Task Handle_Computes_Remaining_As_LessonsCount_Minus_Held()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        await using var db = TestPaymentsDbContextFactory.Create();
        await SeedPackageInvoiceAsync(db, studentId, studyGroupId, issuedOn: new DateOnly(2026, 9, 1), lessonsCount: 10, validDays: 60);

        var attendance = new FakeAttendanceQueryService(heldCount: 6);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today));

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages.Count.ShouldBe(1);
        var package = result.Packages[0];
        package.LessonsCount.ShouldBe(10);
        package.UsedCount.ShouldBe(6);
        package.RemainingCount.ShouldBe(4);
        package.IsExpired.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_Floors_Remaining_At_Zero_When_HeldExceedsLessonsCount()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        await using var db = TestPaymentsDbContextFactory.Create();
        await SeedPackageInvoiceAsync(db, studentId, studyGroupId, issuedOn: new DateOnly(2026, 9, 1), lessonsCount: 5, validDays: 60);

        var attendance = new FakeAttendanceQueryService(heldCount: 8);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today));

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages[0].UsedCount.ShouldBe(8);
        result.Packages[0].RemainingCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_Sets_ExpiresOn_Null_When_ValidDaysIsZero()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        await using var db = TestPaymentsDbContextFactory.Create();
        await SeedPackageInvoiceAsync(db, studentId, studyGroupId, issuedOn: new DateOnly(2026, 1, 1), lessonsCount: 10, validDays: 0);

        var attendance = new FakeAttendanceQueryService(heldCount: 3);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today));

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages[0].ExpiresOn.ShouldBeNull();
        result.Packages[0].IsExpired.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_Marks_Expired_And_Freezes_CountingWindow_At_ExpiresOn()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var issuedOn = new DateOnly(2026, 1, 1);
        await using var db = TestPaymentsDbContextFactory.Create();
        await SeedPackageInvoiceAsync(db, studentId, studyGroupId, issuedOn, lessonsCount: 10, validDays: 30); // expires 2026-01-31

        var attendance = new FakeAttendanceQueryService(heldCount: 7);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today)); // today is far past expiry

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages[0].IsExpired.ShouldBeTrue();
        result.Packages[0].ExpiresOn.ShouldBe(new DateOnly(2026, 1, 31));

        // The counting window must stop at expiry, not run to "today" — sessions held after expiry
        // are never attributed to an already-expired package.
        attendance.Calls.ShouldHaveSingleItem();
        attendance.Calls[0].From.ShouldBe(issuedOn);
        attendance.Calls[0].To.ShouldBe(new DateOnly(2026, 1, 31));
    }

    [Fact]
    public async Task Handle_Reports_Every_PerPackage_Invoice_Independently_Not_Just_One_Active()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        await using var db = TestPaymentsDbContextFactory.Create();
        var (first, _) = await SeedPackageInvoiceAsync(db, studentId, studyGroupId, new DateOnly(2026, 6, 1), lessonsCount: 10, validDays: 400);
        var (second, _) = await SeedPackageInvoiceAsync(db, studentId, studyGroupId, new DateOnly(2026, 9, 1), lessonsCount: 8, validDays: 400);

        var attendance = new FakeAttendanceQueryService(heldCount: 2);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today));

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages.Count.ShouldBe(2);
        result.Packages.ShouldContain(p => p.InvoiceId == first.Id && p.LessonsCount == 10);
        result.Packages.ShouldContain(p => p.InvoiceId == second.Id && p.LessonsCount == 8);
    }

    [Fact]
    public async Task Handle_Excludes_NonPackage_Tariffs()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        await using var db = TestPaymentsDbContextFactory.Create();

        var tariff = Tariff.Create("Per lesson", null, TariffKind.PerLesson, 15m, "USD", 0, 0, false);
        db.Tariffs.Add(tariff);
        var invoice = StudentInvoice.Create("INV-2026-0001", studentId, null, studyGroupId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 5), "USD", null);
        invoice.ReplaceLines([("Per lesson — 5 занятий", tariff.Id, 5m, 15m)]);
        invoice.Issue(new DateOnly(2026, 9, 30));
        db.StudentInvoices.Add(invoice);
        await db.SaveChangesAsync();

        var attendance = new FakeAttendanceQueryService(heldCount: 5);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today));

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_Excludes_ManuallyEdited_MultiLine_Invoices_From_Package_Projection()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        await using var db = TestPaymentsDbContextFactory.Create();

        var tariff = Tariff.Create("10-lesson package", null, TariffKind.PerPackage, 200m, "USD", 10, 60, false);
        db.Tariffs.Add(tariff);
        var invoice = StudentInvoice.Create("INV-2026-0002", studentId, null, studyGroupId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), "USD", null);
        invoice.ReplaceLines(
        [
            ("Package", tariff.Id, 1m, 200m),
            ("Extra material", null, 1m, 20m),
        ]);
        invoice.Issue(new DateOnly(2026, 9, 1));
        db.StudentInvoices.Add(invoice);
        await db.SaveChangesAsync();

        var attendance = new FakeAttendanceQueryService(heldCount: 3);
        var handler = new GetStudentBalanceQueryHandler(db, attendance, new FixedTimeProvider(Today));

        var result = await handler.Handle(new GetStudentBalanceQuery(studentId), CancellationToken.None);

        result.Packages.ShouldBeEmpty();
    }

    private static async Task<(StudentInvoice Invoice, Tariff Tariff)> SeedPackageInvoiceAsync(
        PaymentsDbContext db, Guid studentId, Guid studyGroupId, DateOnly issuedOn, int lessonsCount, int validDays)
    {
        var tariff = Tariff.Create("Package", null, TariffKind.PerPackage, 200m, "USD", lessonsCount, validDays, false);
        db.Tariffs.Add(tariff);

        var invoice = StudentInvoice.Create("INV-2026-0003", studentId, null, studyGroupId, issuedOn, issuedOn, issuedOn.AddDays(7), "USD", null);
        invoice.ReplaceLines([($"Package ({lessonsCount} занятий)", tariff.Id, 1m, 200m)]);
        invoice.Issue(issuedOn);
        db.StudentInvoices.Add(invoice);
        await db.SaveChangesAsync();

        return (invoice, tariff);
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private sealed class FakeAttendanceQueryService(int heldCount) : IAttendanceQueryService
    {
        public List<(Guid StudentId, Guid StudyGroupId, DateOnly From, DateOnly To)> Calls { get; } = [];

        public ValueTask<int> CountHeldSessionsAsync(
            Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default)
        {
            Calls.Add((studentId, studyGroupId, from, toDate));
            return ValueTask.FromResult(heldCount);
        }

        public ValueTask<AttendanceBreakdown> GetBreakdownAsync(
            Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by GetStudentBalanceQueryHandler.");
    }
}
