﻿using MailKit;
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

                var uids = client.Inbox.Search(SearchQuery.SentSince(DateTime.Now.AddDays(-1)));
                var unprocessedUids = lastProcessedMailId == null ? uids : uids.Where(q => q.Id > lastProcessedMailId);
                if (!unprocessedUids.Any()) return lastProcessedMailId;

                _logger.LogInformation("Retrieved '{Count}' new emails to process", unprocessedUids.Count());

                foreach (var uid in unprocessedUids)
                {
                    try
                    {
                        var message = client.Inbox.GetMessage(uid);
                        var body = message.HtmlBody;
                        if (string.IsNullOrWhiteSpace(body)) body = message.TextBody;
                        var summary = await _openAiMailFunctions.GetSummaryFromEmailContent(message.Subject, body);
                        if (summary != null)
                        {
                            var from = message.From.First().ToString();
                            summary.Subject = message.Subject;
                            summary.Contact = from;
                            _sender.AddToCalendarIfNotExisting(summary);
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(10));
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