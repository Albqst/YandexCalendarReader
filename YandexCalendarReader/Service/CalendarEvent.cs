using System.Text.RegularExpressions;

namespace YandexCalendarReader.Service;

public class CalendarEvent
{
    public string Summary { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string Uid { get; set; }
    public string RRule { get; set; }

    public CalendarEvent ParseVEvent(string calendarData)
    {
        var lines = calendarData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var evt = new CalendarEvent();

        foreach (var line in lines)
        {
            if (line.StartsWith("SUMMARY:"))
                evt.Summary = line.Substring("SUMMARY:".Length);

            if (line.StartsWith("DTSTART"))
                evt.Start = ParseDateTime(line);

            if (line.StartsWith("DTEND"))
                evt.End = ParseDateTime(line);

            if (line.StartsWith("DESCRIPTION:"))
                evt.Description = line.Substring("DESCRIPTION:".Length).Replace("\\n", "\n");

            if (line.StartsWith("URL:"))
                evt.Url = line.Substring("URL:".Length);

            if (line.StartsWith("UID:"))
                evt.Uid = line.Substring("UID:".Length);

            if (line.StartsWith("RRULE:"))
                evt.RRule = line.Substring("RRULE:".Length);
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