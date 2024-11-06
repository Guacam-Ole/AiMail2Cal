using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

using System.Data.SqlTypes;

namespace AiMailScanner
{
    public class OpenAiMailFunctions
    {
        private OpenAIService _service;
        private readonly Config _config;

        public OpenAiMailFunctions(Config config)
        {
            _config = config;
            _service = Login();
        }

        private OpenAIService Login()
        {
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = _config.OpenAiSecret
            });
            return openAiService;
        }

        public async Task<Summary?> GetMarkdownSummaryFromEmailContent(string subject, string emailContent)
        {
            var detectDateResult = await _service.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = [
                    ChatMessage.FromSystem("You analyze emails if they contain a date that can be used for a calendar entry."),
                    ChatMessage.FromSystem("You also offer a summary if its content in German language."),
                    ChatMessage.FromSystem("The first line the user is the subject from the mail, then followed by the email body"),
                    ChatMessage.FromSystem("The first line of your response should contain the date in the format \"year-month-day Hour:minute\", Starting at line two a detailed summary in which can contain multiple lines should be returned. The summary should be structured and contain multiple lines for better readability"),
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
                Console.WriteLine($"retrieving summary costs '{detectDateResult.Usage.TotalTokens}'");
                var content = detectDateResult.Choices.First().Message.Content ?? string.Empty;
                var lines = content.Split("\n");
                if (lines.Length < 2)
                {
                    Console.WriteLine($"No valid data for '{subject}'. Most likely no date there");
                    return null;
                }
                var possibleDate = lines[0];
                var possibleSummary = string.Join("\\n", lines[2..]);

                if (!DateTime.TryParse(possibleDate, out DateTime dateFromMail))
                {
                    Console.WriteLine($"No valid date '{possibleDate}' found for '{subject}'");
                    return null;
                }
                if (dateFromMail < DateTime.Now)
                {
                    Console.WriteLine($"Date '{possibleDate}' is in the past for '{subject}'. Will not store it");
                    return null;
                }
                Console.WriteLine($"New entry will be created with date '{possibleDate}' and subject '{subject}'");

                return new Summary
                {
                    ElementDate = dateFromMail,
                    Body = possibleSummary
                };
            }
            else
            {
                Console.WriteLine($"OpenAI failed for detecting content: '{detectDateResult.Error?.Message}'");
            }
            return null;
        }
    }
}