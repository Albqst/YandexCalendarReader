using Microsoft.EntityFrameworkCore;
using YandexCalendarReader.Service;

namespace YandexCalendarReader;

public class EventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveIfNotExistsAsync(CalendarEvent evt)
    {
        var exists = await _db.CalendarEvents.AnyAsync(e =>
            e.Summary == evt.Summary && e.Start == evt.Start && e.End == evt.End);

        if (!exists)
        {
            _db.CalendarEvents.Add(evt);
            await _db.SaveChangesAsync();
        }
    }
}