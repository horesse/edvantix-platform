using System.Net;
using System.Net.Http.Json;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Modules.People.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Quota;

/// <summary>
/// End-to-end wiring check for the plan-limit soft block: the create handlers call
/// <see cref="QuotaGuardExtensions.EnsureHeadroomAsync"/> and the Files upload handler calls
/// <c>IQuotaService.CheckAsync</c>, so a denying quota service must surface as HTTP 402 (entity
/// creation) / 507 (storage) through the real pipeline and exception handler — while reads stay
/// unaffected. Quota enforcement is disabled by config for the suite, so this class swaps in a
/// denying <see cref="IQuotaService"/> for its own host only.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PlanLimitEnforcementTests
{
    private const string PeopleBase = "/api/v1";
    private const string FilesBase = "/api/v1/files";

    private readonly WebApplicationFactory<Program> _denyingFactory;
    private readonly AuthHelper _auth;

    public PlanLimitEnforcementTests(FshWebApplicationFactory factory)
    {
        _denyingFactory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IQuotaService>();
            services.AddScoped<IQuotaService, DenyingQuotaService>();
        }));
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Create_is_blocked_with_402_reads_still_work_and_storage_returns_507()
    {
        var token = await _auth.GetRootAdminTokenAsync();
        using var client = _denyingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Reads are never gated.
        using var listBefore = await client.GetAsync($"{PeopleBase}/students?pageNumber=1&pageSize=1");
        listBefore.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Student create -> guard denies -> 402.
        using var student = await client.PostAsJsonAsync($"{PeopleBase}/students", new
        {
            lastName = $"S-{Guid.NewGuid():N}",
            firstName = "Test",
            middleName = (string?)null,
            birthDate = new DateOnly(2010, 1, 1),
            phone = "+10000000000",
            email = $"s-{Guid.NewGuid():N}@example.com",
            managerUserId = Guid.NewGuid().ToString(),
            source = (string?)null,
        });
        student.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired,
            $"body: {await student.Content.ReadAsStringAsync()}");
        (await student.Content.ReadAsStringAsync()).ShouldContain("ActiveStudents");

        // Teacher create -> 402 too.
        using var teacher = await client.PostAsJsonAsync($"{PeopleBase}/teachers", new
        {
            lastName = $"T-{Guid.NewGuid():N}",
            firstName = "Test",
            middleName = (string?)null,
            phone = "+10000000001",
            email = $"t-{Guid.NewGuid():N}@example.com",
            bio = (string?)null,
            specializations = (string[]?)null,
            hourlyRate = (decimal?)null,
        });
        teacher.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);

        // Files upload URL -> storage quota denied -> 507.
        using var upload = await client.PostAsJsonAsync($"{FilesBase}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName = "notes.pdf",
            contentType = "application/pdf",
            sizeBytes = 500L,
            visibility = 1,
            category = "Document",
        });
        ((int)upload.StatusCode).ShouldBe(507, $"body: {await upload.Content.ReadAsStringAsync()}");

        // Reads still work after the blocked writes — no data was lost.
        using var listAfter = await client.GetAsync($"{PeopleBase}/students?pageNumber=1&pageSize=1");
        listAfter.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = await listAfter.DeserializeAsync<PagedResult<StudentDto>>();
    }

    /// <summary>Denies the gauge resources that the create/upload paths check; everything else
    /// (notably ApiCalls) stays unlimited so unrelated requests are unaffected.</summary>
    private sealed class DenyingQuotaService : IQuotaService
    {
        private static bool IsGated(QuotaResource r) =>
            r is QuotaResource.ActiveStudents or QuotaResource.ActiveTeachers
              or QuotaResource.StudyGroups or QuotaResource.MonthlySessions
              or QuotaResource.StorageBytes;

        public ValueTask<QuotaCheckResult> CheckAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
            => ValueTask.FromResult(IsGated(resource)
                ? new QuotaCheckResult(false, resource, 5, 5, null)
                : QuotaCheckResult.Unlimited(resource, 0));

        public ValueTask<long> RecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
            => ValueTask.FromResult(0L);

        public ValueTask<QuotaCheckResult> CheckAndRecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
            => CheckAsync(tenantId, resource, amount, ct);

        public ValueTask<long> GetCurrentAsync(string tenantId, QuotaResource resource, CancellationToken ct = default)
            => ValueTask.FromResult(0L);
    }
}
