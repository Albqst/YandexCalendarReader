using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YandexCalendarReader.Service;

namespace YandexCalendarReader;

[ApiController]
[Route("api/[controller]")]
public class YandexCalendarReaderController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ReadYandex _reader;
    private readonly AppSettings _settings;

    public YandexCalendarReaderController(AppDbContext db, ReadYandex reader, IOptions<AppSettings> settings)
    {
        _db = db;
        _reader = reader;
        _settings = settings.Value;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncEvents()
    {
        var events = await _reader.ReadAsync(_db, _settings);

        // Преобразуем и сохраняем в базу
        foreach (var evt in events)
        {
            var entity = new CalendarEvent
            {
                summary = evt.summary,
                description = evt.description,
                start = DateTime.SpecifyKind(evt.start, DateTimeKind.Utc),
                end = DateTime.SpecifyKind(evt.end, DateTimeKind.Utc)
            };

            _db.CalendarEvents.Add(entity);
        }

        await _db.SaveChangesAsync();
        return Ok(new { Count = events.Count });
    }
}
