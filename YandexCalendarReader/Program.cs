using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YandexCalendarReader.Service;
using System.Timers;
using Microsoft.EntityFrameworkCore;

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

                // var connectionString = context.Configuration.GetConnectionString("Postgres");
                // При каждом запуске обнуляем AccessToken, чтобы не использовать старый
                settings.AccessToken = null;

                services.AddSingleton(settings);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(context.Configuration.GetConnectionString("Postgres")));
                services.AddSingleton<TokenRefresher>();
                services.AddSingleton<ReadYandex>();
                
                services.AddEndpointsApiExplorer();
                services.AddRouting();
            }).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app =>
                {
                    var readYandex = app.ApplicationServices.GetRequiredService<ReadYandex>();

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Эндпоинт для получения событий
                        endpoints.MapGet("/events", async context =>
                        {
                            var events = await readYandex.GetCalendarEvents("","iroromani@yandex.ru" ,"kuptvxhftiefoauz"); // Твой метод получения событий
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(events);
                        });
                    });
                });

            })
            .Build();

        // Запускаем периодическое обновление токена
        var refresher = host.Services.GetRequiredService<TokenRefresher>();

        _timer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
        _timer.Elapsed += async (sender, e) =>
        {
            try
            {
                await refresher.GetValidAccessTokenAsync();
                Console.WriteLine($"[{DateTime.Now}] Access Token обновлён.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления токена по таймеру: {ex.Message}");
            }
        };
        
        _timer.AutoReset = true;
        _timer.Start();

        await host.RunAsync();

        // Блокируем завершение программы
        Console.WriteLine("Нажмите Enter для выхода...");
        Console.ReadLine();
    }
}