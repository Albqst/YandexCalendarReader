using YandexCalendarReader.Service;
using Microsoft.EntityFrameworkCore;


// TODO: Тех Задание 
// Сотворить подключение к CITYP
// Придумать вариант с горячим обновлением событий после их добавления
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
        
        var builder = WebApplication.CreateBuilder(args);

// Настройки
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        var settings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
        builder.Services.AddSingleton(settings);

// Подключение базы
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// DI
        builder.Services.AddSingleton<TokenRefresher>();
        builder.Services.AddScoped<ReadYandex>();

// Контроллеры и Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();
        
// Создаем миграции
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate(); // Применяет миграции при запуске
        }

// Swagger и ошибки
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

// Middleware
        app.UseRouting();

// Authorization можно включить позже при необходимости
// app.UseAuthorization();

        app.MapControllers();

// Таймер обновления токена
        var refresher = app.Services.GetRequiredService<TokenRefresher>();
        var timer = new System.Timers.Timer(TimeSpan.FromMinutes(100).TotalMilliseconds);
        timer.Elapsed += async (_, _) =>
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
        timer.AutoReset = true;
        timer.Start();

// Запуск приложения
        app.Run("http://localhost:5000");
    }
}