using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Payments.Services;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using NSubstitute;
using Payments.Tests.Features;

namespace Payments.Tests.Services;

/// <summary>
/// EDX-015 — the "block lesson materials while a student is in arrears" rule
/// (<see cref="MaterialsAccessService"/>). Covers the tenant flag, the grace window, who is exempt
/// (teachers / users with no domain identity), the invoice-status filter, and that the check is
/// scoped to the caller's own students (no leakage from another student's debt). The Docker-backed
/// end-to-end block/unblock + tenant isolation lives in Integration.Tests.
/// </summary>
public sealed class MaterialsAccessServiceTests
{
    private static readonly DateOnly Today = new(2026, 9, 20);
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Not_Restricted_When_TenantFlag_Off()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var studentId = Guid.NewGuid();
        await SeedOverdueInvoiceAsync(db, studentId, dueDate: Today.AddDays(-30));

        var svc = Build(db, flag: false, graceDays: 7, scope: StudentScope(studentId));

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeFalse();
        status.GraceDays.ShouldBe(7);
    }

    [Fact]
    public async Task Not_Restricted_For_A_Teacher_Even_With_Overdue_Debt()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var studentId = Guid.NewGuid();
        await SeedOverdueInvoiceAsync(db, studentId, dueDate: Today.AddDays(-30));

        var scope = new PeopleScope(studentId, TeacherId: Guid.NewGuid(), GuardianId: null, WardStudentIds: []);
        var svc = Build(db, flag: true, graceDays: 7, scope: scope);

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeFalse();
    }

    [Fact]
    public async Task Not_Restricted_When_User_Has_No_Domain_Identity()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var svc = Build(db, flag: true, graceDays: 7, scope: PeopleScope.Empty);

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeFalse();
    }

    [Fact]
    public async Task Not_Restricted_While_Still_Inside_The_Grace_Window()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var studentId = Guid.NewGuid();
        // Overdue by 3 days, grace is 7 → not yet blocking.
        await SeedOverdueInvoiceAsync(db, studentId, dueDate: Today.AddDays(-3));

        var svc = Build(db, flag: true, graceDays: 7, scope: StudentScope(studentId));

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeFalse();
    }

    [Fact]
    public async Task Restricted_Once_Overdue_Past_The_Grace_Window()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var studentId = Guid.NewGuid();
        var due = Today.AddDays(-10);
        await SeedOverdueInvoiceAsync(db, studentId, dueDate: due);

        var svc = Build(db, flag: true, graceDays: 7, scope: StudentScope(studentId));

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeTrue();
        status.OverdueSince.ShouldBe(due);
    }

    [Fact]
    public async Task Restricted_For_A_Guardian_Whose_Ward_Is_In_Arrears()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var wardId = Guid.NewGuid();
        await SeedOverdueInvoiceAsync(db, wardId, dueDate: Today.AddDays(-20));

        var scope = new PeopleScope(StudentId: null, TeacherId: null, GuardianId: Guid.NewGuid(), WardStudentIds: [wardId]);
        var svc = Build(db, flag: true, graceDays: 7, scope: scope);

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeTrue();
    }

    [Fact]
    public async Task Not_Restricted_When_The_Only_Past_Due_Invoice_Is_Paid()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var studentId = Guid.NewGuid();

        var invoice = StudentInvoice.Create(studentId, null, Guid.NewGuid(),
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), Today.AddDays(-15), "USD", null);
        invoice.ReplaceLines([("Сентябрь", null, 1m, 100m)]);
        invoice.Issue(new DateOnly(2026, 8, 1));
        invoice.ConfirmPayment(100m, new DateOnly(2026, 8, 2), PaymentMethod.Cash, null, null, "u", null);
        db.StudentInvoices.Add(invoice);
        await db.SaveChangesAsync();

        var svc = Build(db, flag: true, graceDays: 7, scope: StudentScope(studentId));

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeFalse();
    }

    [Fact]
    public async Task Not_Restricted_By_Another_Students_Debt()
    {
        await using var db = TestPaymentsDbContextFactory.Create();
        var me = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();
        await SeedOverdueInvoiceAsync(db, someoneElse, dueDate: Today.AddDays(-40));

        var svc = Build(db, flag: true, graceDays: 7, scope: StudentScope(me));

        var status = await svc.GetForUserAsync(UserId, CancellationToken.None);

        status.Restricted.ShouldBeFalse();
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static PeopleScope StudentScope(Guid studentId) =>
        new(studentId, TeacherId: null, GuardianId: null, WardStudentIds: []);

    private static MaterialsAccessService Build(
        PaymentsDbContext db, bool flag, int graceDays, PeopleScope scope)
    {
        var tenantSettings = Substitute.For<ITenantSettingsService>();
        tenantSettings.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantSettingsDto { RestrictMaterialsOnDebt = flag, DebtGraceDays = graceDays });

        var resolver = Substitute.For<IPeopleScopeResolver>();
        resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(scope);

        return new MaterialsAccessService(db, tenantSettings, resolver, new FixedTimeProvider(Today));
    }

    private static async Task SeedOverdueInvoiceAsync(PaymentsDbContext db, Guid studentId, DateOnly dueDate)
    {
        var invoice = StudentInvoice.Create(studentId, null, Guid.NewGuid(),
            dueDate.AddMonths(-1), dueDate, dueDate, "USD", null);
        invoice.ReplaceLines([("Обучение", null, 1m, 100m)]);
        invoice.Issue(dueDate.AddMonths(-1));
        db.StudentInvoices.Add(invoice);
        await db.SaveChangesAsync();
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
