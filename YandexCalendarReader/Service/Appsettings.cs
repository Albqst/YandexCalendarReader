namespace YandexCalendarReader.Service;
    public class AppSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string Scope { get; set; }
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
        public string PasswordAlbert { get; set; }
        public string NameOfCalendar { get; set; }
        public string CalendarUri { get; set; }
    }