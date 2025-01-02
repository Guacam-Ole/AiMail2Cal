using CalCli;

using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

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

        public void AddToCalendarIfNotExisting(EmailContents summary)
        {
            try
            {
                if (summary.StartDate.Year == 1900) return;

                var davConnection = new BasicConnection(_config.CalDavConfig.UserName, _config.CalDavConfig.Password);
                var davServer = new CalDav.Client.Server(_config.CalDavConfig.Url, davConnection);
                var calendars = davServer.GetCalendars();
                var calendar = _config.CalDavConfig.Calendar == null ? calendars.First() : calendars.First(q => q.Name == _config.CalDavConfig.Calendar);
                var calEvent = new CalDav.Event
                {
                    Start = summary.StartDate,

                    Summary = summary.Subject,
                    Description = summary.Summary,
                    Created = DateTime.Now,
                    Categories = { "OpenAI", "FromMail" },
                    Organizer = new CalDav.Contact { Name = "AiMailScanner" },
                    Location = summary.Location
                };
                if (summary.EndDate > summary.StartDate)
                {
                    calEvent.End = summary.EndDate;
                }
                calEvent.IsAllDay = summary.StartDate.Hour == 0 && calEvent.End != null;

                if (summary.Contact != null)
                {
                    calEvent.Attendees = [new CalDav.Contact { Name = summary.Contact, Email = summary.Contact }];
                }

                calendar.Save(calEvent);

                _logger.LogInformation("Created new appointment on '{AppointmentDate}' with subject '{Subject}'", summary.StartDate, summary.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed creating appointment for '{Summary}'. Will not retry.", summary);
            }
        }
    }
}