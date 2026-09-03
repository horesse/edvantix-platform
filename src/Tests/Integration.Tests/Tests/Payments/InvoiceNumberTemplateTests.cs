using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Payments.Services;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Payments;

/// <summary>
/// EDX-013 — the configurable <c>TenantSettings.InvoiceNumberTemplate</c> and the concurrency-safe
/// per-tenant counter behind <c>StudentInvoice.Number</c>. Verifies against real Postgres that:
/// (1) a template set through <c>PUT /tenants/settings</c> is applied to a freshly created invoice,
/// and (2) 100 numbers drawn in one heavily-parallel burst are unique, gap-free and monotonic —
/// the <c>INSERT … ON CONFLICT DO UPDATE … RETURNING</c> block reservation in
/// <see cref="InvoiceNumberGenerator"/> holds under contention.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class InvoiceNumberTemplateTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public InvoiceNumberTemplateTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task NewInvoice_Should_Take_Its_Number_From_The_Tenant_Template()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        try
        {
            await SetTemplateAsync(client, "SCH-{YY}-{NNNN}");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            using var createResponse = await client.PostAsJsonAsync(
                $"{TestConstants.PaymentsBasePath}/student-invoices",
                new
                {
                    studentId = Guid.NewGuid(),
                    payerGuardianId = (Guid?)null,
                    studyGroupId = (Guid?)null,
                    periodFrom = today,
                    periodTo = today,
                    dueDate = today.AddDays(7),
                    currency = "USD",
                    comment = (string?)null,
                    lines = new[] { new { description = "Tuition", tariffId = (Guid?)null, quantity = 1m, unitPrice = 100m } },
                });
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
                $"create failed: {await createResponse.Content.ReadAsStringAsync()}");
            var invoiceId = await createResponse.DeserializeAsync<Guid>();

            using var getResponse = await client.GetAsync($"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}");
            getResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
                $"get failed: {await getResponse.Content.ReadAsStringAsync()}");

            var detail = await getResponse.DeserializeAsync<InvoiceDetailShape>();
            detail.Number.ShouldMatch($@"^SCH-{today.Year % 100:D2}-\d{{4}}$");
        }
        finally
        {
            await SetTemplateAsync(client, "{YYYY}-{NNNN}");
        }
    }

    [Fact]
    public async Task Concurrent_Number_Generation_Should_Be_Unique_GapFree_And_Monotonic()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // A template with its own dedicated (non-year) counter scope, so the assertions below are
        // exact regardless of what other tests in this collection billed against the default one.
        const string Template = "EDXR-{NNNNNN}";
        var pattern = new Regex(@"^EDXR-(\d{6})$", RegexOptions.CultureInvariant);

        try
        {
            await SetTemplateAsync(client, Template);

            const int Tasks = 20;
            const int PerTask = 5;
            const int Total = Tasks * PerTask;

            var batches = await Task.WhenAll(Enumerable.Range(0, Tasks).Select(_ => Task.Run(async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var sp = scope.ServiceProvider;

                var rootTenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
                    .GetAsync(MultitenancyConstants.Root.Id);
                sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
                    new MultiTenantContext<AppTenantInfo>(rootTenant!);

                var generator = sp.GetRequiredService<IInvoiceNumberGenerator>();
                return await generator.NextBatchAsync(PerTask, CancellationToken.None);
            })));

            var numbers = batches.SelectMany(b => b).ToList();
            numbers.Count.ShouldBe(Total);
            numbers.ShouldAllBe(n => pattern.IsMatch(n));

            var sequences = numbers.Select(n => int.Parse(pattern.Match(n).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
            sequences.Distinct().Count().ShouldBe(Total, "every reserved number must be unique");

            var ordered = sequences.OrderBy(x => x).ToList();
            var start = ordered[0];
            ordered.ShouldBe(Enumerable.Range(start, Total).ToList(), "the block must be contiguous — no gaps, no overlap");

            // Within any single batch the numbers must be strictly ascending (monotonic).
            foreach (var batch in batches)
            {
                var s = batch.Select(n => int.Parse(pattern.Match(n).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
                s.ShouldBe(s.OrderBy(x => x).ToList());
                (s[^1] - s[0]).ShouldBe(PerTask - 1);
            }
        }
        finally
        {
            await SetTemplateAsync(client, "{YYYY}-{NNNN}");
        }
    }

    private static async Task SetTemplateAsync(HttpClient client, string template)
    {
        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/settings",
            new
            {
                timeZoneId = "UTC",
                currency = "USD",
                restrictMaterialsOnDebt = false,
                debtGraceDays = 7,
                invoiceNumberTemplate = template,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"failed to set invoice template: {await response.Content.ReadAsStringAsync()}");
    }

    private sealed record InvoiceDetailShape(Guid Id, string Number);
}
