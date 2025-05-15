using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WebDav;

class Program
{
    static async Task Main()
    {
        // Ваши данные OAuth
        var clientId = "6ef89dd84031421ba20c4645eee9f0b7"; //не менять
        var clientSecret = "3beaeb21839d4a328c798e36284e2611"; // не менять
        var redirectUri = "https://oauth.yandex.ru/verification_code"; // не менять
        var scope = "login:email"; // доступ к календарю
        var PasswordAlbert = "kuptvxhftiefoauz"; // пароль к приложению
        string calendarUri = "";

        try
        {
            Console.WriteLine("===== Яндекс Календарь Reader =====\n");

            // 1. OAuth авторизация
            Console.WriteLine("1. Авторизация в Яндексе");
            var oauthUrl = $"https://oauth.yandex.ru/authorize?response_type=code&client_id={clientId}&scope={scope}";
            Console.WriteLine($"Откройте в браузере:\n{oauthUrl}");

            Console.Write("\nВведите код авторизации: ");
            var authCode = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(authCode))
            {
                Console.WriteLine("Ошибка: Код авторизации пустой.");
                return;
            }

            using var clientToken = new HttpClient();

            Console.WriteLine("\n2. Запрашиваем access_token...");
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            });

            var tokenResponse = await clientToken.PostAsync("https://oauth.yandex.ru/token", tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка получения токена: {tokenResponse.StatusCode}");
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                Console.WriteLine(errorContent);
                return;
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            File.WriteAllText("token.json", tokenJson); // Прописать как поднимать токен!!!


            // Прописать как поднимать токен!!!// Прописать как поднимать токен!!!
            // Прописать как поднимать токен!!!// Прописать как поднимать токен!!!

            var tokenData = System.Text.Json.JsonDocument.Parse(tokenJson);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

            Console.WriteLine("Токен успешно получен.\n");

            //2 
            var client = new WebDavClient(new WebDavClientParams
            {
                BaseAddress = new Uri("https://caldav.yandex.ru"),
                Credentials = new NetworkCredential("iroromani@yandex.ru", PasswordAlbert)
            });

            var calendarsPath = "/calendars/iroromani@yandex.ru/";
            var result = await client.Propfind(calendarsPath);

            var loader = new Load();
            calendarUri =
                loader.GetTargetCalendarUri(result, calendarUri); // Получили ссылку на целевой календарь с событиями

            // 3. Запрос REPORT-запрос типа calendar-query с фильтром по дате по календарю Алиса
            var responseXmlList = await loader.GetCalendarEvents(calendarUri, "iroromani@yandex.ru", PasswordAlbert);

            // string calendarData2 = "...твой текст между BEGIN:VCALENDAR ... END:VCALENDAR...";
            var parser = new CalendarEvent();
            foreach (var stroke in responseXmlList)
            {
                var evt = parser.ParseVEvent(stroke);

                Console.WriteLine($"Событие: {evt.Summary}");
                Console.WriteLine($"Время: {evt.Start} — {evt.End}");
                Console.WriteLine($"Описание: {evt.Description}");
                Console.WriteLine($"Повтор: {evt.RRule}");
                Console.WriteLine($"Ссылка: {evt.Url}");
            }

            Console.WriteLine("Получилось!");
        }
        catch (Exception ex){
            Console.WriteLine(ex.Message);
        }
    }

/*
// 4. Парсинг событий
Console.WriteLine("\n4. Обработка полученных событий:");

var xdoc = XDocument.Parse(null);

var nsDav = XNamespace.Get("DAV:");
var nsCaldav = XNamespace.Get("urn:ietf:params:xml:ns:caldav");

var events = new List<CalendarEvents>();

foreach (var response in xdoc.Descendants(nsDav + "response"))
{
    var calendarData = response.Descendants(nsCaldav + "calendar-data").FirstOrDefault();
    if (calendarData != null)
    {
        var parsedEvent = CalendarEvents.ParseFromICS(calendarData.Value);
        if (parsedEvent != null)
        {
            events.Add(parsedEvent);
        }
    }
}

if (events.Count == 0)
{
    Console.WriteLine("На выбранную дату событий нет.");
}
else
{
    foreach (var e in events)
    {
        Console.WriteLine($"- [{e.Start:HH:mm}] {e.Summary}");
    }
}

Console.WriteLine("\nГотово!");
}
catch (Exception ex)
{
Console.WriteLine($"\nПроизошла ошибка: {ex.Message}");
}

}
}*/


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



    public class CalendarEvents
    {
        public DateTime Start { get; set; }
        public string Summary { get; set; } = "";

        public static CalendarEvents? ParseFromICS(string icsData)
        {
            try
            {
                var lines = icsData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var evt = new CalendarEvents();

                foreach (var line in lines)
                {
                    if (line.StartsWith("DTSTART"))
                    {
                        var dateStr = line.Split(':')[1];
                        if (DateTime.TryParseExact(dateStr, "yyyyMMddTHHmmssZ", null,
                                System.Globalization.DateTimeStyles.AdjustToUniversal, out var start))
                        {
                            evt.Start = start;
                        }
                    }
                    else if (line.StartsWith("SUMMARY"))
                    {
                        evt.Summary = line.Split(':')[1];
                    }
                }

                return evt.Start != default ? evt : null;
            }
            catch
            {
                return null;
            }
        }

    }

    public class Load
    {
        public string GetTargetCalendarUri(PropfindResponse result, string calendarUri)
        {
            foreach (var resource in result.Resources)
            {
                Console.WriteLine($"Resource: {resource.Uri}");
                foreach (var prop in resource.Properties)
                {
                    calendarUri = prop.Value == "Алисa" ? resource.Uri : "Не найден календарь с именем Алиса!";
                    if (calendarUri.StartsWith("/calendars/iroromani"))
                    {
                        return calendarUri;
                    }

                    Console.WriteLine($"  {prop.Name}: {prop.Value}");
                }
            }

            return calendarUri;
        }

        public async Task<List<string>> GetCalendarEvents(string calendarUri, string login, string appPassword)
        {
            using var handler = new HttpClientHandler { Credentials = new NetworkCredential(login, appPassword) };
            using var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://caldav.yandex.ru");

            // Установим интервал даты: 14 мая 2025, с 00:00 до 23:59
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
        <C:time-range start=""20250515T000000Z"" end=""20250516T000000Z""/>
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
}
