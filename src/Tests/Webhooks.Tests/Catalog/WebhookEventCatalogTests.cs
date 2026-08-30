using FSH.Modules.Webhooks.Contracts.Catalog;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookEventCatalog;
using FSH.Modules.Webhooks.Features.v1.GetWebhookEventCatalog;

namespace Webhooks.Tests.Catalog;

public sealed class WebhookEventCatalogTests
{
    [Fact]
    public void All_Should_Be_NonEmpty_With_Unique_Names()
    {
        WebhookEventCatalog.All.ShouldNotBeEmpty();
        WebhookEventCatalog.All
            .Select(e => e.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ShouldBe(WebhookEventCatalog.All.Count);
    }

    [Fact]
    public void All_Entries_Should_Have_Module_And_Description()
    {
        WebhookEventCatalog.All.ShouldAllBe(e =>
            !string.IsNullOrWhiteSpace(e.Module) && !string.IsNullOrWhiteSpace(e.Description));
    }

    [Fact]
    public void Catalogued_Names_Should_Follow_IntegrationEvent_Naming()
    {
        // The name doubles as the fan-out selector (typeof(TEvent).Name), so every entry must be a
        // simple integration-event type name.
        WebhookEventCatalog.All.ShouldAllBe(e => e.Name.EndsWith("IntegrationEvent", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("StudentEnrolledIntegrationEvent", true)]
    [InlineData("studentenrolledintegrationevent", true)]
    [InlineData("*", true)]
    [InlineData("user.created", false)]
    [InlineData("", false)]
    public void IsKnownSelector_Should_Accept_Catalogued_Names_And_Wildcard(string value, bool expected)
        => WebhookEventCatalog.IsKnownSelector(value).ShouldBe(expected);

    [Fact]
    public async Task Handler_Should_Return_The_Catalog()
    {
        var handler = new GetWebhookEventCatalogQueryHandler();

        var result = await handler.Handle(new GetWebhookEventCatalogQuery(), CancellationToken.None);

        result.ShouldBe(WebhookEventCatalog.All);
    }
}
