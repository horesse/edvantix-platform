using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Notifications.Tests.Channels;

public sealed class EmailNotificationChannelTests
{
    private static NotificationDelivery Delivery(RenderedNotification content, string? email) =>
        new("u1", email, "t", "Notifications", content, Metadata: null);

    private static readonly RenderedNotification WithEmail =
        new("Title", "Body", null, "Subject", "<p>Body</p>");

    private static readonly RenderedNotification WithoutEmail =
        new("Title", "Body", null, null, null);

    [Fact]
    public async Task Sends_When_Template_Has_Email_And_Address_Present()
    {
        var mail = Substitute.For<IMailService>();
        var channel = new EmailNotificationChannel(mail, NullLogger<EmailNotificationChannel>.Instance);

        await channel.SendAsync(Delivery(WithEmail, "parent@example.com"));

        await mail.Received(1).SendAsync(
            Arg.Is<MailRequest>(r => r != null && r.Subject == "Subject" && r.To.Contains("parent@example.com")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoOps_When_Template_Has_No_Email_Body()
    {
        var mail = Substitute.For<IMailService>();
        var channel = new EmailNotificationChannel(mail, NullLogger<EmailNotificationChannel>.Instance);

        await channel.SendAsync(Delivery(WithoutEmail, "parent@example.com"));

        await mail.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task NoOps_When_Recipient_Has_No_Address()
    {
        var mail = Substitute.For<IMailService>();
        var channel = new EmailNotificationChannel(mail, NullLogger<EmailNotificationChannel>.Instance);

        await channel.SendAsync(Delivery(WithEmail, email: null));

        await mail.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Swallows_Transport_Failure()
    {
        var mail = Substitute.For<IMailService>();
        mail.SendAsync(Arg.Any<MailRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("smtp down"));
        var channel = new EmailNotificationChannel(mail, NullLogger<EmailNotificationChannel>.Instance);

        await Should.NotThrowAsync(() => channel.SendAsync(Delivery(WithEmail, "parent@example.com")));
    }
}
