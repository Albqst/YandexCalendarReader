using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YandexCalendarReader.Service;
using System.Timers;

// TODO: Тех Задание 
// Сотворить подключение к CITYP
// Придумать второй таймер работающий по будним дням по РАСПИСАНИЮ(может потом прикручу кастомные даты чтобы обыграть праздники)
// Добавить больше безопасности при работе с УДП проверки существования события проверки отработанного времени. Как это делать конечно большой вопрос.
// 
// ------------------------
//      ╱ ╱▔▔▔▔▔▔▔╲ 
//       ╱          ╲ 
//      ▕            ▕ 
//      ▕╭━╮╮╭━╮┣╯   ▕ 
//      ▕┃▕▋┊┃▕▋╰╮    ▏
//      ▕╰━╭╮╰━╯    ╰┈▏
//      ▕▂╮┗┛   ╭┳┳┳╯▕ 
//      ^v^ ┳┳┳┳┳┫╰┃▂╱ 
//      ^v^▕╋╋╋╋┫┃▕╯ 
//      ^v^▕┻┻┻┻┻╯▕ 
class Program
{
    private static System.Timers.Timer? _timer;

    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Загружаем конфиг
                var settings = Loader.Load("appsettings.json");

                // При каждом запуске обнуляем AccessToken, чтобы не использовать старый
                settings.AccessToken = null;

                services.AddSingleton(settings);
                services.AddSingleton<TokenRefresher>();
                services.AddSingleton<ReadYandex>();
            })
            .Build();

        var refresher = host.Services.GetRequiredService<TokenRefresher>();

        // Получаем токен сразу
        var accessToken = await refresher.GetValidAccessTokenAsync();
        Console.WriteLine("Access Token получен"); // Установить время обновления токена при условии что приложение
        // работает в фоне и понять может ли быть проблема вход в сон компа если логика будет оставлена на локальном ПК

        // Запускаем таймер на 5 минут срабатывание - доработать логику рефреш и поулчение токена
        _timer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _timer.Elapsed += async (sender, e) =>
        {
            try
            {
                var token = await refresher.GetValidAccessTokenAsync();
                Console.WriteLine($"[{DateTime.Now}] Access Token обновлён.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления токена по таймеру: {ex.Message}");
            }
        };
        _timer.AutoReset = true;
        _timer.Start();

        // Блокируем завершение программы
        Console.WriteLine("Нажмите Enter для выхода...");
        Console.ReadLine();
    }
}