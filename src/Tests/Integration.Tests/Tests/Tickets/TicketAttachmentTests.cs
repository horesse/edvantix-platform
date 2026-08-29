using System.Security.Cryptography;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Ticket attachments go through the generic Files endpoints with <c>ownerType=Ticket</c>; the only
/// gate is <c>TicketFileAccessPolicy</c> (reporter/assignee attach + read, uploader deletes).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketAttachmentTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public TicketAttachmentTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Reporter_Can_Attach_Read_And_Delete_A_Ticket_File()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateTicketAsync(client);

        // Attach (upload-url must be granted for ownerType=Ticket / ownerId=<my ticket>).
        byte[] bytes = new byte[512];
        RandomNumberGenerator.Fill(bytes);
        var presigned = await RequestUploadUrlAsync(client, ticketId, bytes.Length);

        using (var raw = new HttpClient())
        using (var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes) { Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") } },
        })
        {
            (await raw.SendAsync(put)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var finalize = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalize.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Read (CanRead → reporter).
        using var url = await client.GetAsync($"{FilesBasePath}/{presigned.FileAssetId}/url");
        url.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Delete (CanDelete → uploader).
        using var del = await client.DeleteAsync($"{FilesBasePath}/{presigned.FileAssetId}");
        del.IsSuccessStatusCode.ShouldBeTrue($"delete returned {del.StatusCode}");
    }

    [Fact]
    public async Task Attach_Is_Denied_For_A_Ticket_The_Caller_Does_Not_Participate_In()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "Ticket",
            ownerId = Guid.NewGuid(), // no such ticket → not a participant
            fileName = "evidence.pdf",
            contentType = "application/pdf",
            sizeBytes = 128,
            visibility = 1,
            category = "Document",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> CreateTicketAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = $"Attach-{Guid.NewGuid():N}",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<PresignedUploadResponse> RequestUploadUrlAsync(HttpClient client, Guid ticketId, long size)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "Ticket",
            ownerId = ticketId,
            fileName = "evidence.pdf",
            contentType = "application/pdf",
            sizeBytes = size,
            visibility = 1,
            category = "Document",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<PresignedUploadResponse>();
    }
}
