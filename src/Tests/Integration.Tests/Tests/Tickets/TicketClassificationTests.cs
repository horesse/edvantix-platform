using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Covers ticket category + audience (docs/02 Модули/Tickets.md → «Применение в Edvantix»): the
/// category-driven default audience, an explicit audience override, re-classification on update,
/// and the new search filters.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketClassificationTests
{
    private readonly AuthHelper _auth;

    public TicketClassificationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CreateTicket_Should_DefaultTo_General_School()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("Plain"));

        var fetched = await GetAsync(client, ticketId);
        fetched.Category.ShouldBe("General");
        fetched.Audience.ShouldBe("School");
    }

    [Fact]
    public async Task CreateTicket_Technical_Should_RouteTo_Platform_ByDefault()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var create = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Bug"),
            category = "Technical",
        });
        var ticketId = await create.DeserializeAsync<Guid>();

        var fetched = await GetAsync(client, ticketId);
        fetched.Category.ShouldBe("Technical");
        fetched.Audience.ShouldBe("Platform");
    }

    [Fact]
    public async Task CreateTicket_Should_Honor_Explicit_Audience_Override()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var create = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Escalate"),
            category = "Payment",
            audience = "Platform",
        });
        var ticketId = await create.DeserializeAsync<Guid>();

        var fetched = await GetAsync(client, ticketId);
        fetched.Category.ShouldBe("Payment");
        fetched.Audience.ShouldBe("Platform");
    }

    [Fact]
    public async Task UpdateTicket_Should_Recompute_Audience_From_New_Category()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var create = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Reclassify"),
            category = "Technical",
        });
        var ticketId = await create.DeserializeAsync<Guid>();

        var update = await client.PutAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}", new
        {
            title = "Reclassify (edited)",
            description = (string?)null,
            priority = "Medium",
            category = "Schedule",
        });
        update.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, ticketId);
        fetched.Category.ShouldBe("Schedule");
        fetched.Audience.ShouldBe("School");
    }

    [Fact]
    public async Task SearchTickets_Should_FilterBy_Category_And_Audience()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var techCreate = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("TechFilter"),
            category = "Technical",
        });
        var techId = await techCreate.DeserializeAsync<Guid>();
        var schoolId = await CreateAsync(client, UniqueTitle("SchoolFilter"));

        var byCategory = await client.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets?category=Technical&pageSize=200");
        var categoryPage = await byCategory.DeserializeAsync<PagedResult<TicketDto>>();
        categoryPage.Items.ShouldContain(t => t.Id == techId);
        categoryPage.Items.ShouldNotContain(t => t.Id == schoolId);

        var byAudience = await client.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets?audience=Platform&pageSize=200");
        var audiencePage = await byAudience.DeserializeAsync<PagedResult<TicketDto>>();
        audiencePage.Items.ShouldContain(t => t.Id == techId);
        audiencePage.Items.ShouldNotContain(t => t.Id == schoolId);
    }

    private static string UniqueTitle(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<Guid> CreateAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new { title });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<TicketDto> GetAsync(HttpClient client, Guid ticketId)
    {
        var response = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<TicketDto>();
    }
}
