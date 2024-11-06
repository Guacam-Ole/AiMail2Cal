using CalCli;

namespace AiMailScanner
{
    public class DavSender
    {
        private readonly Config _config;

        public DavSender(Config config)
        {
            _config = config;
        }

        public void AddToCalendarIfNotExisting(Summary summary)
        {
            var davConnection = new BasicConnection(_config.CalDavConfig.UserName, _config.CalDavConfig.Password);
            var davServer = new CalDav.Client.Server(_config.CalDavConfig.Url, davConnection);
            var calendars = davServer.GetCalendars();
            var calendar = _config.CalDavConfig.Calendar == null ? calendars.First() : calendars.First(q => q.Name == _config.CalDavConfig.Calendar);
            calendar.Save(new CalDav.Event
            {
                Start = summary.ElementDate,
                Summary = summary.Subject,
                Description = summary.Body,
                Created = DateTime.Now,
                Categories = { "OpenAI", "FromMail" },
                Organizer = new CalDav.Contact { Name = "AiMailScanner" },
                Attendees = [new CalDav.Contact { Name = summary.From }]
            });
        }
    }
}