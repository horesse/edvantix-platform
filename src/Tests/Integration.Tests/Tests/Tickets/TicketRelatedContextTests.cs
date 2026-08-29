using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Covers the optional context links added so a ticket can point at the student / study group /
/// invoice it is about (docs/02 Модули/Tickets.md → «Применение в Edvantix»): set on create,
/// replaced on update, and usable as search filters (the student card's ticket history).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketRelatedContextTests
{
    private readonly AuthHelper _auth;

    public TicketRelatedContextTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CreateTicket_Should_PersistRelatedContext()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var studentId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var create = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Refund"),
            relatedStudentId = studentId,
            relatedStudyGroupId = groupId,
            relatedInvoiceId = invoiceId,
        });
        create.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ticketId = await create.DeserializeAsync<Guid>();

        var fetched = await GetAsync(client, ticketId);
        fetched.RelatedStudentId.ShouldBe(studentId);
        fetched.RelatedStudyGroupId.ShouldBe(groupId);
        fetched.RelatedInvoiceId.ShouldBe(invoiceId);
    }

    [Fact]
    public async Task CreateTicket_Should_RejectEmptyGuidContext()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var create = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("BadContext"),
            relatedStudentId = Guid.Empty,
        });

        create.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTicket_Should_ReplaceRelatedContext()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var create = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Recontext"),
            relatedStudentId = Guid.NewGuid(),
        });
        var ticketId = await create.DeserializeAsync<Guid>();

        var newStudentId = Guid.NewGuid();
        var update = await client.PutAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}", new
        {
            title = "Recontext (edited)",
            description = (string?)null,
            priority = "Medium",
            relatedStudentId = newStudentId,
        });
        update.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, ticketId);
        fetched.RelatedStudentId.ShouldBe(newStudentId);
        fetched.RelatedStudyGroupId.ShouldBeNull("update omitting a link clears it (full-replace PUT)");
    }

    [Fact]
    public async Task SearchTickets_Should_FilterBy_RelatedInvoiceId()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var invoiceId = Guid.NewGuid();

        var withInvoice = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("HasInvoice"),
            relatedInvoiceId = invoiceId,
        });
        var withInvoiceId = await withInvoice.DeserializeAsync<Guid>();

        var withoutInvoiceId = await CreateAsync(client, UniqueTitle("NoInvoice"));

        var response = await client.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets?relatedInvoiceId={invoiceId}&pageSize=200");
        var page = await response.DeserializeAsync<PagedResult<TicketDto>>();

        page.Items.ShouldContain(t => t.Id == withInvoiceId);
        page.Items.ShouldNotContain(t => t.Id == withoutInvoiceId);
    }

    private static string UniqueTitle(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<Guid> CreateAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets", new { title });
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
