using Microsoft.EntityFrameworkCore;

namespace YandexCalendarReader.Service;

public class AppDbContext : DbContext
{
    public DbSet<CalendarEvent> CalendarEvents { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CalendarEvent>().ToTable("calendar_events");
    }
}