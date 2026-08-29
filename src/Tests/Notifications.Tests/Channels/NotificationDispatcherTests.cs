using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Notifications.Tests.Channels;

public sealed class NotificationDispatcherTests
{
    private static readonly IReadOnlyDictionary<string, string?> NoTokens =
        new Dictionary<string, string?>(StringComparer.Ordinal);

    private static NotificationDispatcher Build(
        out RecordingChannel inApp,
        out RecordingChannel email,
        string? ambientTenant = "acme")
    {
        inApp = new RecordingChannel(NotificationChannelKind.InApp);
        email = new RecordingChannel(NotificationChannelKind.Email);

        var renderer = Substitute.For<INotificationTemplateRenderer>();
        renderer.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string?>>())
            .Returns(new RenderedNotification("Title", "Body", "/link", "Subject", "<p>Body</p>"));

        var tenantInfo = ambientTenant is null ? null : new AppTenantInfo { Id = ambientTenant, Identifier = ambientTenant, Name = ambientTenant };
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        var context = Substitute.For<IMultiTenantContext<AppTenantInfo>>();
        context.TenantInfo.Returns(tenantInfo);
        accessor.MultiTenantContext.Returns(context);

        return new NotificationDispatcher(
            [inApp, email], renderer, accessor, NullLogger<NotificationDispatcher>.Instance);
    }

    [Fact]
    public async Task Dispatch_Fans_Out_To_All_Requested_Channels()
    {
        var dispatcher = Build(out var inApp, out var email);

        await dispatcher.DispatchAsync(new NotificationRequest("u1", "t", NoTokens)
        {
            Channels = NotificationChannelKind.All,
        });

        inApp.Deliveries.Count.ShouldBe(1);
        email.Deliveries.Count.ShouldBe(1);
        inApp.Deliveries[0].RecipientUserId.ShouldBe("u1");
        inApp.Deliveries[0].Content.Title.ShouldBe("Title");
    }

    [Fact]
    public async Task Dispatch_Skips_Channels_Not_In_The_Request()
    {
        var dispatcher = Build(out var inApp, out var email);

        await dispatcher.DispatchAsync(new NotificationRequest("u1", "t", NoTokens)
        {
            Channels = NotificationChannelKind.InApp,
        });

        inApp.Deliveries.Count.ShouldBe(1);
        email.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Dispatch_Throws_On_Tenant_Mismatch()
    {
        var dispatcher = Build(out var inApp, out _, ambientTenant: "acme");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(new NotificationRequest("u1", "t", NoTokens)
            {
                ExpectedTenantId = "globex",
            }));

        inApp.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Dispatch_Allows_Matching_Expected_Tenant()
    {
        var dispatcher = Build(out var inApp, out _, ambientTenant: "acme");

        await dispatcher.DispatchAsync(new NotificationRequest("u1", "t", NoTokens)
        {
            ExpectedTenantId = "acme",
        });

        inApp.Deliveries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Dispatch_Passes_Source_Through_To_Delivery()
    {
        var dispatcher = Build(out var inApp, out _);

        await dispatcher.DispatchAsync(new NotificationRequest("u1", "t", NoTokens)
        {
            Source = "Scheduling",
            Channels = NotificationChannelKind.InApp,
        });

        inApp.Deliveries[0].Source.ShouldBe("Scheduling");
    }

    private sealed class RecordingChannel(NotificationChannelKind kind) : INotificationChannel
    {
        public NotificationChannelKind Kind => kind;

        public List<NotificationDelivery> Deliveries { get; } = [];

        public Task SendAsync(NotificationDelivery delivery, CancellationToken ct = default)
        {
            Deliveries.Add(delivery);
            return Task.CompletedTask;
        }
    }
}
