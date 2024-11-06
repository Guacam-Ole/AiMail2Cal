using MailKit.Search;
using MailKit;

using System.Text;
using MailKit.Net.Imap;

namespace AiMailScanner
{
    public class ImapReceiver
    {
        private readonly Config _config;
        private readonly OpenAiMailFunctions _openAiMailFunctions;
        private readonly DavSender _sender;

        public ImapReceiver(Config config, OpenAiMailFunctions openAiMailFunctions, DavSender sender)
        {
            _config = config;
            _openAiMailFunctions = openAiMailFunctions;
            _sender = sender;
        }

        public async Task<UniqueId?> ReceiveUnCheckedEmails(UniqueId? lastProcessedMailId)
        {
           //UniqueId? lastReadId = null;
            using (ImapClient client = new ImapClient())
            {
                client.Connect(_config.ImapConfig.Url, _config.ImapConfig.Port, true);
                client.Authenticate(_config.ImapConfig.UserName, _config.ImapConfig.Password);

                client.Inbox.Open(FolderAccess.ReadOnly);

                var uids = client.Inbox.Search(SearchQuery.SentSince(DateTime.Now.AddDays(-2)));

                foreach (var uid in uids)
                {
                    if (lastProcessedMailId != null && uid <= lastProcessedMailId) continue;

                    var message = client.Inbox.GetMessage(uid);
                    var body = message.HtmlBody;
                    if (string.IsNullOrWhiteSpace(body)) body = message.TextBody;
                    var summary = await _openAiMailFunctions.GetMarkdownSummaryFromEmailContent(message.Subject, body);
                    if (summary != null)
                    {
                        var from = message.From.First();

                        summary.Subject = message.Subject;
                        summary.From = from.Name ?? from.ToString();
                        _sender.AddToCalendarIfNotExisting(summary);
                    }
                    lastProcessedMailId = uid;
                }
            }

            return lastProcessedMailId;

            //uint lastId = 0;
            //using var imapClient = new ImapClient(_config.ImapConfig.Url, _config.ImapConfig.Port, _config.ImapConfig.UserName, _config.ImapConfig.Password, AuthMethod.Login, true);
            //var inbox = imapClient.GetMailboxInfo(imapClient.DefaultMailbox);

            //var newMails = imapClient.Search(lastProcessedMailId == null ? SearchCondition.SentSince(DateTime.Today.AddDays(-1)) : SearchCondition.GreaterThan(lastProcessedMailId.Value));
            //var messages = imapClient.GetMessages(newMails);

            //foreach (var message in messages)
            //{
            //    var htmlView = message.AlternateViews.FirstOrDefault(q => q.ContentType.MediaType == "text/html");
            //    string emailContent = htmlView == null ? message.Body : Encoding.UTF8.GetString(((MemoryStream)htmlView.ContentStream).ToArray());

            //    var summary = await _openAiMailFunctions.GetMarkdownSummaryFromEmailContent(message.Subject, emailContent);
            //    if (summary != null)
            //    {
            //        summary.Subject = $"{message.Sender}:{message.Subject}";
            //        _sender.AddToCalendarIfNotExisting(summary);
            //    }
            //}
        }
    }
}