using System.Globalization;
using System.Text;

namespace FSH.Modules.Scheduling.Services;

/// <summary>One VEVENT worth of data, timezone-free (everything is UTC — DTSTART/DTEND carry the
/// trailing <c>Z</c>, so the calendar client renders them in the viewer's own zone, which for a
/// school feed matches the school's zone).</summary>
internal sealed record IcalEvent(
    string Uid,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Summary,
    string? Location,
    string? Description,
    bool Cancelled,
    DateTimeOffset? LastModifiedUtc);

/// <summary>Minimal RFC 5545 VCALENDAR serialiser — enough for a read-only "subscribe to my
/// schedule" feed (PUBLISH method, no VTIMEZONE because every stamp is UTC). Handles the two things
/// hand-rolled iCal usually gets wrong: CRLF line endings and 75-octet line folding, plus TEXT
/// escaping.</summary>
internal static class IcalWriter
{
    private const string Crlf = "\r\n";

    public static string Build(string calendarName, IReadOnlyList<IcalEvent> events, DateTimeOffset stampUtc)
    {
        var sb = new StringBuilder();
        AppendLine(sb, "BEGIN:VCALENDAR");
        AppendLine(sb, "VERSION:2.0");
        AppendLine(sb, "PRODID:-//Edvantix//Scheduling//EN");
        AppendLine(sb, "CALSCALE:GREGORIAN");
        AppendLine(sb, "METHOD:PUBLISH");
        AppendLine(sb, $"X-WR-CALNAME:{EscapeText(calendarName)}");

        var stamp = FormatUtc(stampUtc);
        foreach (var e in events)
        {
            AppendLine(sb, "BEGIN:VEVENT");
            AppendLine(sb, $"UID:{e.Uid}");
            AppendLine(sb, $"DTSTAMP:{stamp}");
            AppendLine(sb, $"DTSTART:{FormatUtc(e.StartUtc)}");
            AppendLine(sb, $"DTEND:{FormatUtc(e.EndUtc)}");
            AppendLine(sb, $"SUMMARY:{EscapeText(e.Summary)}");
            if (!string.IsNullOrWhiteSpace(e.Location))
            {
                AppendLine(sb, $"LOCATION:{EscapeText(e.Location)}");
            }
            if (!string.IsNullOrWhiteSpace(e.Description))
            {
                AppendLine(sb, $"DESCRIPTION:{EscapeText(e.Description)}");
            }
            AppendLine(sb, $"STATUS:{(e.Cancelled ? "CANCELLED" : "CONFIRMED")}");
            if (e.LastModifiedUtc is { } modified)
            {
                AppendLine(sb, $"LAST-MODIFIED:{FormatUtc(modified)}");
            }
            AppendLine(sb, "END:VEVENT");
        }

        AppendLine(sb, "END:VCALENDAR");
        return sb.ToString();
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string EscapeText(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace("\r\n", "\\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\r", "\\n", StringComparison.Ordinal);

    /// <summary>Append a content line, folded to 75 octets per RFC 5545 §3.1 (continuations start
    /// with a single space).</summary>
    private static void AppendLine(StringBuilder sb, string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line);
        if (bytes.Length <= 75)
        {
            sb.Append(line).Append(Crlf);
            return;
        }

        var start = 0;
        var first = true;
        while (start < bytes.Length)
        {
            // Leave room for the leading space on continuation lines.
            var max = first ? 75 : 74;
            var take = Math.Min(max, bytes.Length - start);

            // Don't split a multi-byte UTF-8 sequence: back off while the next byte is a continuation byte.
            while (take > 0 && start + take < bytes.Length && (bytes[start + take] & 0xC0) == 0x80)
            {
                take--;
            }

            if (!first)
            {
                sb.Append(' ');
            }
            sb.Append(Encoding.UTF8.GetString(bytes, start, take)).Append(Crlf);
            start += take;
            first = false;
        }
    }
}
