using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YandexCalendarReader.Service;

// TODO: Тех Задание 
// Сотворить подключение к CITYP 
// В ближайших планах сделать программу работающую в фоне 
// Причесать код
// Запихнуть = инкапсулировать где нужно
// Добавить больше переменных чтобы Пользователь кастомизировал Время Шедулера А в будущем и Разброс Дней которые Шедулер будет проверять(В будущих версиях добавится - для этого нужна механика проверки существования "отработанного времени")
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
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Загружаем конфиг
                var settings = Loader.Load("appsettings.json");
                services.AddSingleton(settings);

                // Добавляем TokenRefresher
                services.AddSingleton<TokenRefresher>();
                services.AddSingleton<ReadYandex>();
            })
            .Build();

        var refresher = host.Services.GetRequiredService<TokenRefresher>();
        var accessToken = await refresher.GetValidAccessTokenAsync();
        
        // Console.WriteLine("Access Token: " + accessToken);
    }
}
