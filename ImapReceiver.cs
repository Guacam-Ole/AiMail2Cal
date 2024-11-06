using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;

using Microsoft.Extensions.Logging;

namespace AiMailScanner
{
    public class ImapReceiver
    {
        private readonly Config _config;
        private readonly OpenAiMailFunctions _openAiMailFunctions;
        private readonly DavSender _sender;
        private readonly ILogger<ImapReceiver> _logger;

        public ImapReceiver(Config config, OpenAiMailFunctions openAiMailFunctions, DavSender sender, ILogger<ImapReceiver> logger)
        {
            _config = config;
            _openAiMailFunctions = openAiMailFunctions;
            _sender = sender;
            _logger = logger;
        }

        public async Task<uint?> ReceiveUnCheckedEmails(uint? lastProcessedMailId)
        {
            using (var client = new ImapClient())
            {
                try
                {
                    client.Connect(_config.ImapConfig.Url, _config.ImapConfig.Port, true);
                    client.Authenticate(_config.ImapConfig.UserName, _config.ImapConfig.Password);
                    client.Inbox.Open(FolderAccess.ReadOnly);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot connect to Imap server. Check Credentials and configuration. Program will stop");
                    throw;
                }

                var uids = client.Inbox.Search(SearchQuery.SentSince(DateTime.Now.AddDays(-2)));

                foreach (var uid in uids)
                {
                    try
                    {
                        if (lastProcessedMailId != null && uid.Id <= lastProcessedMailId) continue;

                        var message = client.Inbox.GetMessage(uid);
                        var body = message.HtmlBody;
                        if (string.IsNullOrWhiteSpace(body)) body = message.TextBody;
                        var summary = await _openAiMailFunctions.GetMarkdownSummaryFromEmailContent(message.Subject, body);
                        if (summary != null)
                        {
                            var from = message.From.First();

                            summary.Subject = message.Subject;
                            summary.Contact = from;
                            _sender.AddToCalendarIfNotExisting(summary);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed reading email with id '{uid}' from imapserver. Will discard that mail and continue with next one.", uid);
                    }
                    finally
                    {
                        lastProcessedMailId = uid.Id;
                    }
                }
            }

            return lastProcessedMailId;
        }
    }
}