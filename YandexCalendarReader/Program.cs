using YandexCalendarReader.Service;
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
                services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

                var settings = context.Configuration.GetSection("AppSettings").Get<AppSettings>();
                
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
                        endpoints.MapGet("/events", async context =>
                        {
                            var settings = context.RequestServices.GetRequiredService<AppSettings>();

                            var events = await readYandex.ReadAsync(settings);

                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(events);
                        });
                    });
                });
                webBuilder.UseUrls("http://localhost:5000");

            })
            .Build();

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

        Console.WriteLine("Нажмите Enter для выхода...");
        Console.ReadLine();
    }
}