using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using WebDav;

namespace YandexCalendarReader.Service;

public static class Loader
{
    public static AppSettings Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        if (settings == null)
            throw new Exception("Не удалось загрузить настройки.");

        return settings;
    }
    
    public static void Save(AppSettings settings, string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(filePath, json);
    }
    
    public static string GetTargetCalendarUri(PropfindResponse result, string calendarUri, string nameCalendar)
    {
        foreach (var resource in result.Resources)
        {
            Console.WriteLine($"Resource: {resource.Uri}");
            foreach (var prop in resource.Properties)
            {
                calendarUri = prop.Value == nameCalendar ? resource.Uri : "Не найден календарь с именем Алиса!";
                if (calendarUri.StartsWith("/calendars/iroromani"))
                {
                    return calendarUri;
                }

                Console.WriteLine($"  {prop.Name}: {prop.Value}");
            }
        }

        return calendarUri;
    }
    
    public static async Task<List<string>> GetCalendarEvents(string calendarUri, string login, string appPassword)
        {
            using var handler = new HttpClientHandler { Credentials = new NetworkCredential(login, appPassword) };
            using var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://caldav.yandex.ru");

            // Установить интервал даты: 14 мая 2025, с 00:00 до 23:59
            var startDate = new DateTime(2025, 5, 15);
            var endDate = new DateTime(2025, 5, 16);

            // Формат в стиле "yyyyMMddTHHmmssZ"
            string startUtc = startDate.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
            string endUtc = endDate.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
            
            var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<C:calendar-query xmlns:C=""urn:ietf:params:xml:ns:caldav""
                  xmlns:D=""DAV:"">
  <D:prop>
    <D:getetag/>
    <C:calendar-data/>
  </D:prop>
  <C:filter>
    <C:comp-filter name=""VCALENDAR"">
      <C:comp-filter name=""VEVENT"">
        <C:time-range start=""{startUtc}"" end=""{endUtc}""/>
      </C:comp-filter>
    </C:comp-filter>
  </C:filter>
</C:calendar-query>";

            var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
            var request = new HttpRequestMessage(new HttpMethod("REPORT"), calendarUri)
            {
                Content = content
            };
            request.Headers.Add("Depth", "1");

            var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Парсинг всех блоков <calendar-data>
            var result = new List<string>();
            var xdoc = XDocument.Parse(responseBody);

            foreach (var data in xdoc.Descendants().Where(x => x.Name.LocalName == "calendar-data"))
            {
                result.Add(data.Value);
            }

            return result;
        }
}