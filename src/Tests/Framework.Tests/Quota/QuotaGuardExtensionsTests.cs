using System.Net;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;

namespace Framework.Tests.Quota;

public sealed class QuotaGuardExtensionsTests
{
    private const string Tenant = "acme";

    [Fact]
    public async Task EnsureHeadroomAsync_Should_NotThrow_When_CheckAllowed()
    {
        var quotas = Substitute.For<IQuotaService>();
        quotas.CheckAsync(Tenant, QuotaResource.ActiveStudents, 1, Arg.Any<CancellationToken>())
            .Returns(new QuotaCheckResult(true, QuotaResource.ActiveStudents, 4, 50, null));

        await Should.NotThrowAsync(() =>
            quotas.EnsureHeadroomAsync(Tenant, QuotaResource.ActiveStudents).AsTask());
    }

    [Fact]
    public async Task EnsureHeadroomAsync_Should_Throw402_When_CheckNotAllowed()
    {
        var quotas = Substitute.For<IQuotaService>();
        quotas.CheckAsync(Tenant, QuotaResource.ActiveStudents, 1, Arg.Any<CancellationToken>())
            .Returns(new QuotaCheckResult(false, QuotaResource.ActiveStudents, 50, 50, null));

        var ex = await Should.ThrowAsync<QuotaExceededException>(() =>
            quotas.EnsureHeadroomAsync(Tenant, QuotaResource.ActiveStudents).AsTask());

        ex.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
        ex.Resource.ShouldBe(QuotaResource.ActiveStudents);
        ex.CurrentUsage.ShouldBe(50);
        ex.Limit.ShouldBe(50);
        ex.Message.ShouldContain("50/50");
    }

    [Fact]
    public async Task EnsureHeadroomAsync_Should_PassAmountThrough()
    {
        var quotas = Substitute.For<IQuotaService>();
        quotas.CheckAsync(Tenant, QuotaResource.MonthlySessions, 5, Arg.Any<CancellationToken>())
            .Returns(new QuotaCheckResult(true, QuotaResource.MonthlySessions, 10, 500, null));

        await quotas.EnsureHeadroomAsync(Tenant, QuotaResource.MonthlySessions, 5);

        await quotas.Received(1)
            .CheckAsync(Tenant, QuotaResource.MonthlySessions, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureHeadroomAsync_Should_Reject_BlankTenant()
    {
        var quotas = Substitute.For<IQuotaService>();

        await Should.ThrowAsync<ArgumentException>(() =>
            quotas.EnsureHeadroomAsync("  ", QuotaResource.ActiveStudents).AsTask());
    }
}
