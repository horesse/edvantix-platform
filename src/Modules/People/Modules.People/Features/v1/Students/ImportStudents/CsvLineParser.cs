using System.Text;

namespace FSH.Modules.People.Features.v1.Students.ImportStudents;

/// <summary>
/// Minimal RFC4180-ish CSV field splitter (quoted fields, "" escaping for embedded quotes,
/// commas inside quotes). No external package — the import format is small and fixed-column,
/// not worth a CsvHelper dependency for one feature.
/// </summary>
internal static class CsvLineParser
{
    public static IReadOnlyList<string> ParseLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i += 2;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }

            i++;
        }

        fields.Add(sb.ToString());
        return fields;
    }
}
