using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using WebDav;

namespace YandexCalendarReader.Service;

public class TokenRefresher
{
    private const string TokenUrl = "https://oauth.yandex.ru/token";
    private readonly AppSettings _settings;
    private readonly string _settingsFilePath;

    public TokenRefresher(AppSettings settings, string settingsFilePath = "appsettings.json")
    {
        _settings = settings;
        _settingsFilePath = settingsFilePath;
    }

    public async Task<string> RefreshAccessTokenAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.RefreshToken))
                throw new Exception("Нет refresh_token. Получите его через авторизацию с code.");

            using var client = new HttpClient();
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _settings.RefreshToken },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret }
            });

            var response = await client.PostAsync(TokenUrl, tokenRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка обновления токена: {content}");

            var doc = JsonDocument.Parse(content);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            _settings.AccessToken = accessToken;

            if (doc.RootElement.TryGetProperty("refresh_token", out var newRefresh))
            {
                _settings.RefreshToken = newRefresh.GetString();
            }

            Loader.Save(_settings, _settingsFilePath);
            File.WriteAllText("token.json", content);

            //2 
            var clientWebDavClient = new WebDavClient(new WebDavClientParams
            {
                BaseAddress = new Uri("https://caldav.yandex.ru"),
                Credentials = new NetworkCredential("iroromani@yandex.ru", _settings.PasswordAlbert)
            });

            var calendarsPath = "/calendars/iroromani@yandex.ru/";
            var result = await clientWebDavClient.Propfind(calendarsPath);
        
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
        
            // Проверим, есть ли новый refresh_token
            // if (evt.RootElement.TryGetProperty("refresh_token", out var newRefresh))
            // {
            // _settings.RefreshToken = newRefresh.GetString();
            // Loader.Save(_settings, _settingsFilePath);
            // Console.WriteLine("Обновлён refresh_token и сохранён в appsettings.json");
            // }

            Console.WriteLine("Получилось и работает!");
            
            return accessToken!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обновлении токена: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GetValidAccessTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            // Здесь можно добавить проверку срока действия токена, если ты его где-то сохраняешь с меткой времени.
            return _settings.AccessToken;
        }

        return await RefreshAccessTokenAsync();
    }
}