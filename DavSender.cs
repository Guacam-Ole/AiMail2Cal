using CalCli;

using Microsoft.Extensions.Logging;

namespace AiMailScanner
{
    public class DavSender
    {
        private readonly Config _config;
        private readonly ILogger<DavSender> _logger;

        public DavSender(Config config, ILogger<DavSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void AddToCalendarIfNotExisting(Summary summary)
        {
            try
            {
                var davConnection = new BasicConnection(_config.CalDavConfig.UserName, _config.CalDavConfig.Password);
                var davServer = new CalDav.Client.Server(_config.CalDavConfig.Url, davConnection);
                var calendars = davServer.GetCalendars();
                var calendar = _config.CalDavConfig.Calendar == null ? calendars.First() : calendars.First(q => q.Name == _config.CalDavConfig.Calendar);
                var calEvent = new CalDav.Event
                {
                    Start = summary.ElementDate,
                    IsAllDay = summary.ElementDate.Hour == 0,

                    Summary = summary.Subject,
                    Description = summary.Body,
                    Created = DateTime.Now,
                    Categories = { "OpenAI", "FromMail" },
                    Organizer = new CalDav.Contact { Name = "AiMailScanner" }
                };

                if (!calEvent.IsAllDay)
                {
                    calEvent.End = summary.ElementDate.AddMinutes(5);
                }
                if (summary.Contact != null)
                {
                    calEvent.Attendees = [new CalDav.Contact { Name = summary.Contact.Name, Email = summary.Contact.ToString() }];
                }

                calendar.Save(calEvent);

                _logger.LogInformation("Created new appointment on '{AppointmentDate}' with subject '{Subject}'", summary.ElementDate, summary.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed creating appointment for '{Summary}'. Will not retry.", summary);
            }
        }
    }
}