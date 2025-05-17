using System.Net;
using WebDav;

namespace YandexCalendarReader.Service;

// БЕЗ ЭТОГО КЛАССА ОБОШЕЛСЯ
public class ReadYandex
{
    private ReadYandex(AppSettings settings, string settingsFilePath = "appsettings.json")
    {
        _settings = settings;
        _settingsFilePath = settingsFilePath;
    }

    // Ваши данные OAuth
    private const string TokenUrl = "https://oauth.yandex.ru/token";
    private static AppSettings _settings;
    private static string _settingsFilePath;

    // 1. OAuth авторизация
    private bool result = ReadAsync(_settings, _settingsFilePath, TokenUrl).Result;

    private static async Task<bool> ReadAsync(AppSettings settings, string settingsFilePath, string httpsOauthYandexRuToken)
    {
        try
        {
            Console.WriteLine("===== Яндекс Календарь Reader =====\n");

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
}