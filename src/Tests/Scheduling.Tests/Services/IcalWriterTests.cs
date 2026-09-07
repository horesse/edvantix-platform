using System.Text;
using FSH.Modules.Scheduling.Services;

namespace Scheduling.Tests.Services;

public sealed class IcalWriterTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_Wraps_Events_In_A_Valid_Vcalendar_Envelope()
    {
        var ics = IcalWriter.Build("Test", [SampleEvent()], Stamp);

        ics.ShouldStartWith("BEGIN:VCALENDAR\r\n");
        ics.ShouldContain("VERSION:2.0\r\n");
        ics.ShouldContain("PRODID:-//Edvantix//Scheduling//EN\r\n");
        ics.ShouldContain("METHOD:PUBLISH\r\n");
        ics.TrimEnd().ShouldEndWith("END:VCALENDAR");
        CountOccurrences(ics, "BEGIN:VEVENT").ShouldBe(1);
        CountOccurrences(ics, "END:VEVENT").ShouldBe(1);
    }

    [Fact]
    public void Build_Uses_Crlf_Line_Endings_Everywhere()
    {
        var ics = IcalWriter.Build("Test", [SampleEvent()], Stamp);

        // No bare LF that isn't preceded by CR.
        for (var i = 0; i < ics.Length; i++)
        {
            if (ics[i] == '\n')
            {
                ics[i - 1].ShouldBe('\r');
            }
        }
    }

    [Fact]
    public void Build_Formats_Utc_Timestamps_With_Trailing_Z()
    {
        var e = SampleEvent() with
        {
            StartUtc = new DateTimeOffset(2026, 9, 8, 15, 30, 0, TimeSpan.Zero),
            EndUtc = new DateTimeOffset(2026, 9, 8, 17, 0, 0, TimeSpan.Zero),
        };

        var ics = IcalWriter.Build("Test", [e], Stamp);

        ics.ShouldContain("DTSTART:20260908T153000Z\r\n");
        ics.ShouldContain("DTEND:20260908T170000Z\r\n");
        ics.ShouldContain("DTSTAMP:20260907T120000Z\r\n");
    }

    [Fact]
    public void Build_Converts_Non_Utc_Offsets_To_Utc()
    {
        var e = SampleEvent() with
        {
            StartUtc = new DateTimeOffset(2026, 9, 8, 18, 0, 0, TimeSpan.FromHours(3)), // 15:00Z
            EndUtc = new DateTimeOffset(2026, 9, 8, 19, 0, 0, TimeSpan.FromHours(3)),
        };

        var ics = IcalWriter.Build("Test", [e], Stamp);

        ics.ShouldContain("DTSTART:20260908T150000Z\r\n");
        ics.ShouldContain("DTEND:20260908T160000Z\r\n");
    }

    [Fact]
    public void Build_Escapes_Text_Special_Characters()
    {
        var e = SampleEvent() with { Summary = "Math; Algebra, part 1\nnext line" };

        var ics = IcalWriter.Build("Test", [e], Stamp);

        ics.ShouldContain("SUMMARY:Math\\; Algebra\\, part 1\\nnext line\r\n");
    }

    [Fact]
    public void Build_Folds_Long_Lines_At_75_Octets_With_Leading_Space()
    {
        var e = SampleEvent() with { Summary = new string('x', 200) };

        var ics = IcalWriter.Build("Test", [e], Stamp);

        foreach (var line in ics.Split("\r\n"))
        {
            Encoding.UTF8.GetByteCount(line).ShouldBeLessThanOrEqualTo(75);
        }

        // Continuation lines start with a single space.
        var summaryIdx = ics.IndexOf("SUMMARY:", StringComparison.Ordinal);
        var afterFirstFold = ics.IndexOf("\r\n", summaryIdx, StringComparison.Ordinal) + 2;
        ics[afterFirstFold].ShouldBe(' ');
    }

    [Fact]
    public void Build_Marks_Cancelled_Events()
    {
        var e = SampleEvent() with { Cancelled = true };

        var ics = IcalWriter.Build("Test", [e], Stamp);

        ics.ShouldContain("STATUS:CANCELLED\r\n");
    }

    [Fact]
    public void Build_Omits_Optional_Fields_When_Absent()
    {
        var e = SampleEvent() with { Location = null, Description = null, LastModifiedUtc = null };

        var ics = IcalWriter.Build("Test", [e], Stamp);

        ics.ShouldNotContain("LOCATION:");
        ics.ShouldNotContain("DESCRIPTION:");
        ics.ShouldNotContain("LAST-MODIFIED:");
    }

    private static IcalEvent SampleEvent() => new(
        Uid: "session-1@edvantix",
        StartUtc: new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero),
        EndUtc: new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero),
        Summary: "Group A — Algebra",
        Location: "Room 12",
        Description: "https://meet.example/abc",
        Cancelled: false,
        LastModifiedUtc: new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
