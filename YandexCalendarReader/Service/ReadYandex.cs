using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using WebDav;

namespace YandexCalendarReader.Service;

// БЕЗ ЭТОГО КЛАССА ОБОШЕЛСЯ
public class ReadYandex
{
    private readonly AppSettings _settings;
    private readonly TokenRefresher _refresher;
    private readonly string _settingsFilePath = "appsettings.json"; // константа внутри класса

    public ReadYandex(AppSettings settings, TokenRefresher refresher)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _refresher = refresher ?? throw new ArgumentNullException(nameof(refresher)); 
        ReadAsync(_settings, _settingsFilePath, TokenUrl).GetAwaiter().GetResult();
    }


    // Ваши данные OAuth
    private const string TokenUrl = "https://oauth.yandex.ru/token";

    // 1. OAuth авторизация
    

    private async Task<bool> ReadAsync(AppSettings settings, string settingsFilePath, string httpsOauthYandexRuToken)
    {
        try
        {
            Console.WriteLine("===== Яндекс Календарь Reader =====\n");

            List<CalendarEvent> listEvents  = [];
            // 1. OAuth авторизация
            Console.WriteLine("1. Авторизация в Яндексе");
            var oauthUrl = $"https://oauth.yandex.ru/authorize?response_type=code&client_id={settings.ClientId}&scope={settings.Scope}";
            Console.WriteLine($"Откройте в браузере:\n{oauthUrl}");

            Console.Write("\nВведите код авторизации: ");
            var authCode = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(authCode))
            {
                Console.WriteLine("Ошибка: Код авторизации пустой.");
            }

            using var clientToken = new HttpClient();

            Console.WriteLine("\n2. Запрашиваем access_token...");
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret }
                /*
                 * { "grant_type", "refresh_token" },
                   { "refresh_token", "<your_refresh_token>" },
                   { "client_id", "<your_client_id>" },
                   { "client_secret", "<your_client_secret>" }  //Раскомментить чтобы попробовать автообновление Токена
                 */
            });

            var tokenResponse = await clientToken.PostAsync("https://oauth.yandex.ru/token", tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка получения токена: {tokenResponse.StatusCode}");
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                Console.WriteLine(errorContent);
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
                Credentials = new NetworkCredential("iroromani@yandex.ru", _settings.PasswordAlbert)
            });

            var calendarsPath = "/calendars/iroromani@yandex.ru/";
            var result = await client.Propfind(calendarsPath);
            
            _settings.CalendarUri =
                Loader.GetTargetCalendarUri(
                    result,
                    _settings.CalendarUri,
                    _settings.NameOfCalendar); // Получили ссылку на целевой календарь с событиями

            // 3. Запрос REPORT-запрос типа calendar-query с фильтром по дате по календарю Алиса
            var responseXmlList =
                await Loader.GetCalendarEvents(
                    _settings.CalendarUri,
                    "iroromani@yandex.ru", // вынесли в апсетинги или в код
                    _settings.PasswordAlbert);

            var parser = new CalendarEvent();
            foreach (var stroke in responseXmlList)
            {
                var evt = parser.ParseVEvent(stroke);
                listEvents.Add(evt);

                Console.WriteLine($"Событие: {evt.Summary}");
                Console.WriteLine($"Время: {evt.Start} — {evt.End}");
                Console.WriteLine($"Описание: {evt.Description}");
                Console.WriteLine($"Повтор: {evt.RRule}");
                Console.WriteLine($"Ссылка: {evt.Url}");
            }

            Console.WriteLine("Получилось и работает!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return true; // Придумать валидацию
    }
    
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
    
    public async Task<List<string>> GetCalendarEvents(string calendarUri, string login, string appPassword)
        {
            using var handler = new HttpClientHandler { Credentials = new NetworkCredential(login, appPassword) };
            using var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://caldav.yandex.ru");

            // Установить интервал даты: 14 мая 2025, с 00:00 до 23:59
            var startDate = new DateTime(2025, 7, 28);
            var endDate = new DateTime(2025, 7, 30);

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