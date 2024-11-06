using Microsoft.Extensions.Logging;

using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace AiMailScanner
{
    public class OpenAiMailFunctions
    {
        private OpenAIService _service;
        private readonly Config _config;
        private readonly ILogger<OpenAiMailFunctions> _logger;

        public OpenAiMailFunctions(Config config, ILogger<OpenAiMailFunctions> logger)
        {
            _config = config;
            _logger = logger;
            _service = Login();
        }

        private OpenAIService Login()
        {
            try
            {
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = _config.OpenAiSecret
                });
                return openAiService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed connecting to OpenAi-Service. Check Credentials. Program will stop");
                throw;
            }
        }

        public async Task<Summary?> GetSummaryFromEmailContent(string subject, string emailContent)
        {
            try
            {
                var detectDateResult = await _service.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = [
                          ChatMessage.FromSystem("You analyze emails if they contain a date that can be used for a calendar entry."),
                    ChatMessage.FromSystem("You also offer a summary if its content in German language."),
                    ChatMessage.FromSystem("The first line the user is the subject from the mail, then followed by the email body"),
                    ChatMessage.FromSystem("The first line of your response should contain the date in the format \"year-month-day Hour:minute\", Starting at line two a detailed summary"),
                    ChatMessage.FromSystem("If no date can be found simply respond with an empty response instead."),

                    ChatMessage.FromUser(
                    [
                        MessageContent.TextContent(subject),
                        MessageContent.TextContent(emailContent)
                    ])
                     ],
                    Model = Models.Gpt_4o_mini,
                    Temperature = 0.2f,
                    MaxTokens = 400
                });

                if (detectDateResult.Successful)
                {
                    _logger.LogDebug("Retrieving summary costs '{Token}' Tokens", detectDateResult.Usage.TotalTokens);
                    var content = detectDateResult.Choices.First().Message.Content ?? string.Empty;
                    var lines = content.Split("\n");
                    if (lines.Length < 2)
                    {
                        _logger.LogInformation("OpenAi detected no valid date for '{Subject}'. Most likely no date in that mail. Will not create an appointment.", subject);
                        return null;
                    }
                    var possibleDate = lines[0];
                    var possibleSummary = string.Join(" ", lines[2..]);

                    if (!DateTime.TryParse(possibleDate, out DateTime dateFromMail))
                    {
                        _logger.LogInformation("OpenAi retrieved the date '{PossibleDate}' but it cannot be parsed for '{Subject}'. Will ignore this mail", possibleDate, subject);
                        return null;
                    }
                    if (dateFromMail < DateTime.Now)
                    {
                        _logger.LogInformation("OpenAi retrieved the date '{possibleDate}' for '{subject}' but it is in the past. Will not store it", possibleDate, subject);
                        return null;
                    }

                    return new Summary
                    {
                        ElementDate = dateFromMail,
                        Body = possibleSummary
                    };
                }
                else
                {
                    _logger.LogError("OpenAI failed for detecting content with code '{Code}': '{Message}'", detectDateResult.Error?.Code, detectDateResult.Error?.Message);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAi failed. Will ignore this element and continue with next one");
                return null;
            }
        }
    }
}