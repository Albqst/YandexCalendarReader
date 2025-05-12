using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

// Ваши данные OAuth
var clientId = "6ef89dd84031421ba20c4645eee9f0b7";
var clientSecret = "3beaeb21839d4a328c798e36284e2611";
var redirectUri = "https://oauth.yandex.ru/verification_code";
var scope = "login:email"; // доступ к календарю

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

    using var client = new HttpClient();

    Console.WriteLine("\n2. Запрашиваем access_token...");
    var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "grant_type", "authorization_code" },
        { "code", authCode },
        { "client_id", clientId },
        { "client_secret", clientSecret }
    });

    var tokenResponse = await client.PostAsync("https://oauth.yandex.ru/token", tokenRequest);
    if (!tokenResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Ошибка получения токена: {tokenResponse.StatusCode}");
        var errorContent = await tokenResponse.Content.ReadAsStringAsync();
        Console.WriteLine(errorContent);
        return;
    }

    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
    var tokenData = System.Text.Json.JsonDocument.Parse(tokenJson);
    var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

    Console.WriteLine("Токен успешно получен.\n");

    // 2. Запрос событий
    Console.WriteLine("3. Чтение событий из Календаря...");

    var date = DateTime.UtcNow.Date; // можно поменять дату
    var start = date.ToString("yyyyMMdd") + "T000000Z";
    var end = date.ToString("yyyyMMdd") + "T235959Z";

    //Первый запрос (найти путь к календарю)
    var pathToCalendar = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<propfind xmlns=\"DAV:\">\n  <prop>\n    <current-user-principal/>\n  </prop>\n</propfind>";
    
    var caldavRequestBody = $"""
<?xml version="1.0" encoding="utf-8" ?>
<c:calendar-query xmlns:c="urn:ietf:params:xml:ns:caldav">
  <d:prop xmlns:d="DAV:">
    <d:getetag/>
    <c:calendar-data/>
  </d:prop>
  <c:filter>
    <c:comp-filter name="VCALENDAR">
      <c:comp-filter name="VEVENT">
        <c:time-range start="{start}" end="{end}"/>
      </c:comp-filter>
    </c:comp-filter>
  </c:filter>
</c:calendar-query>
""";

    //Пробую получить путь к календарю
    var pathRequest = new HttpRequestMessage(new HttpMethod("REPORT"), "https://caldav.yandex.ru/dav/")
    {
        Content = new StringContent(pathToCalendar, Encoding.UTF8, "application/xml")
    };
    
    var response2 = await client.GetAsync("https://caldav.yandex.ru/dav/");
    if (response2.IsSuccessStatusCode)
    {
        Console.WriteLine("Успешно подключились!");
        var content = await response2.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }
    else
    {
        Console.WriteLine($"Ошибка: {response2.StatusCode}");
        var errorContent = await response2.Content.ReadAsStringAsync();
        Console.WriteLine(errorContent);
    }
    
    var caldavRequest = new HttpRequestMessage(new HttpMethod("REPORT"), "https://caldav.yandex.ru/calendars/Iroromani@yandex.ru/")
    {
        Content = new StringContent(caldavRequestBody, Encoding.UTF8, "application/xml")
    };
    caldavRequest.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);
    caldavRequest.Headers.Add("Depth", "1");

    var caldavResponse = await client.SendAsync(caldavRequest);
    if (!caldavResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Ошибка запроса CalDAV: {caldavResponse.StatusCode}");
        var errorContent = await caldavResponse.Content.ReadAsStringAsync();
        Console.WriteLine(errorContent);
        return;
    }

    var caldavContent = await caldavResponse.Content.ReadAsStringAsync();

    // 3. Парсинг событий
    Console.WriteLine("\n4. Обработка полученных событий:");

    var xdoc = XDocument.Parse(caldavContent);

    var nsDav = XNamespace.Get("DAV:");
    var nsCaldav = XNamespace.Get("urn:ietf:params:xml:ns:caldav");

    var events = new List<CalendarEvent>();

    foreach (var response in xdoc.Descendants(nsDav + "response"))
    {
        var calendarData = response.Descendants(nsCaldav + "calendar-data").FirstOrDefault();
        if (calendarData != null)
        {
            var parsedEvent = CalendarEvent.ParseFromICS(calendarData.Value);
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

public class CalendarEvent
{
    public DateTime Start { get; set; }
    public string Summary { get; set; } = "";

    public static CalendarEvent? ParseFromICS(string icsData)
    {
        try
        {
            var lines = icsData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var evt = new CalendarEvent();

            foreach (var line in lines)
            {
                if (line.StartsWith("DTSTART"))
                {
                    var dateStr = line.Split(':')[1];
                    if (DateTime.TryParseExact(dateStr, "yyyyMMddTHHmmssZ", null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var start))
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
