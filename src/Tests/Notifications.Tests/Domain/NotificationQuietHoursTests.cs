using FSH.Modules.Notifications.Domain;

namespace Notifications.Tests.Domain;

public sealed class NotificationQuietHoursTests
{
    [Theory]
    [InlineData(22, 0, true)]   // inside an evening→morning window
    [InlineData(3, 0, true)]
    [InlineData(7, 59, true)]
    [InlineData(8, 0, false)]   // end is exclusive
    [InlineData(20, 59, false)]
    [InlineData(21, 0, true)]   // start is inclusive
    public void Contains_Handles_Window_That_Spans_Midnight(int hour, int minute, bool expected)
    {
        var qh = NotificationQuietHours.Create(enabled: true, new TimeOnly(21, 0), new TimeOnly(8, 0));

        qh.Contains(new TimeOnly(hour, minute)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(12, 30, true)]
    [InlineData(8, 59, false)]
    [InlineData(17, 0, false)]  // end exclusive
    public void Contains_Handles_Same_Day_Window(int hour, int minute, bool expected)
    {
        var qh = NotificationQuietHours.Create(enabled: true, new TimeOnly(9, 0), new TimeOnly(17, 0));

        qh.Contains(new TimeOnly(hour, minute)).ShouldBe(expected);
    }

    [Fact]
    public void Contains_Is_False_When_Disabled_Or_Zero_Width()
    {
        NotificationQuietHours.Create(enabled: false, new TimeOnly(21, 0), new TimeOnly(8, 0))
            .Contains(new TimeOnly(23, 0)).ShouldBeFalse();

        NotificationQuietHours.Create(enabled: true, new TimeOnly(8, 0), new TimeOnly(8, 0))
            .Contains(new TimeOnly(8, 0)).ShouldBeFalse();
    }
}
