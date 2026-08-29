using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;

namespace Notifications.Tests.Templating;

public sealed class NotificationDefaultsTests
{
    [Fact]
    public void InApp_Is_On_By_Default_For_Every_Type()
    {
        foreach (var type in NotificationTemplateCatalog.Keys)
        {
            NotificationDefaults.IsOn(type, NotificationChannelKind.InApp).ShouldBeTrue(type);
        }
    }

    [Theory]
    [InlineData(NotificationTypes.SessionCancelled, true)]
    [InlineData(NotificationTypes.SessionRescheduled, true)]
    [InlineData(NotificationTypes.InvoiceIssued, true)]
    [InlineData(NotificationTypes.InvoiceOverdue, true)]
    [InlineData(NotificationTypes.SessionReminder, false)]
    [InlineData(NotificationTypes.PaymentConfirmed, false)]
    [InlineData(NotificationTypes.EnrolledInGroup, false)]
    [InlineData(NotificationTypes.AttendanceUnexcused, false)]
    public void Email_Default_Is_On_Only_For_The_High_Signal_Types(string type, bool expected)
    {
        NotificationDefaults.IsOn(type, NotificationChannelKind.Email).ShouldBe(expected);
    }
}
