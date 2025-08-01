using System.Text.RegularExpressions;

namespace YandexCalendarReader.Service;

public class CalendarEvent
{
    public int id { get; set; }
    public string summary { get; set; }
    public DateTime start { get; set; }
    public DateTime end { get; set; }
    public string description { get; set; }

    public CalendarEvent ParseVEvent(string calendarData)
    {
        var lines = calendarData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var evt = new CalendarEvent();

        foreach (var line in lines)
        {
            if (line.StartsWith("SUMMARY:"))
                evt.summary = line.Substring("SUMMARY:".Length);

            if (line.StartsWith("DTSTART"))
                evt.start = ParseDateTime(line);

            if (line.StartsWith("DTEND"))
                evt.end = ParseDateTime(line);

            if (line.StartsWith("DESCRIPTION:"))
                evt.description = line.Substring("DESCRIPTION:".Length).Replace("\\n", "\n");
            
        }

        return evt;
    }

    private DateTime ParseDateTime(string line)
    {
        var match = Regex.Match(line, @":(\d{8}T\d{6})");
        if (match.Success && DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd'T'HHmmss", null,
                System.Globalization.DateTimeStyles.AssumeLocal, out var dt))
        {
            return dt;
        }

        return DateTime.MinValue;
    }
}