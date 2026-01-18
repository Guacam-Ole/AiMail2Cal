using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.Utilities.FunctionCalling;

namespace AiMailScanner
{
    public class OpenAiMailFunctions
    {
        private readonly OpenAIService _service;
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

        public async Task<EmailContents?> GetSummaryFromEmailContent(string subject, string emailContent)
        {
            try
            {
                var rateLimitRetriesLeft = 2;
                var isRateLimited = false;
                var firstAttempt = true;

                ChatCompletionCreateResponse? detectDateResult = null;

                ResponseFormat responseFormat = new()
                {
                    Type = StaticValues.CompletionStatics.ResponseFormat.JsonSchema,
                    JsonSchema = new()
                    {
                        Name = "summary",
                        Strict = false,

                        Schema = PropertyDefinitionGenerator.GenerateFromType(typeof(EmailContents))
                    }
                };

                while (isRateLimited || firstAttempt)
                {
                    firstAttempt = false;
                    detectDateResult = await _service.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                    {
                        Messages = [
                            ChatMessage.FromSystem("You analyze emails if they contain contents that could be used for an appointment for a calendar entry."),
                            ChatMessage.FromSystem("Try to retrieve a StartDate, EndDate, Location and create a summary of the content that should not be longer than 200 characters. The summary should be in German"),
                            ChatMessage.FromSystem("Make sure you only collect useful appointments. For example ignore all emails that contain spam, just inform about a payment date and so on."),
                            ChatMessage.FromSystem("Store all dates in Iso format. If a Date is missing respond with the first day of 1900 instead"),

                            ChatMessage.FromUser(
                            [
                                MessageContent.TextContent(subject),
                                MessageContent.TextContent(emailContent)
                            ])
                                 ],
                        Model = Models.Gpt_4o_mini,
                        Temperature = 0.2f,
                        MaxTokens = 400,
                        ResponseFormat = responseFormat
                    });

                    if (!detectDateResult.Successful)
                    {
                        isRateLimited = detectDateResult.Error?.Code == "rate_limit_exceeded";
                        if (isRateLimited)
                        {
                            _logger.LogWarning("openai rate limit exceeded. Will wait one minute before I retry. '{retries}' retries left", rateLimitRetriesLeft);
                            Thread.Sleep(TimeSpan.FromSeconds(60));
                            rateLimitRetriesLeft--;
                            if (rateLimitRetriesLeft == 0) break;
                        }
                    }
                }

                if (detectDateResult != null && detectDateResult.Successful)
                {
                    _logger.LogDebug("Retrieving summary costs '{Token}' Tokens", detectDateResult.Usage.TotalTokens);
                    var content = detectDateResult.Choices.First().Message.Content;
                    if (content == null)
                    {
                        _logger.LogError("OpenAi Returned no result");
                        return null;
                    }
                    EmailContents? result;
                    try
                    {
                        result = JsonConvert.DeserializeObject<EmailContents>(content, new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat });
                        if (result == null)
                        {
                            _logger.LogError("No valid response from OpenAi. Will ignore this mail");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No valid response from OpenAi. Will ignore this mail");
                        return null;
                    }

                    if (result.StartDate < DateTime.Now.AddHours(4))
                    {
                        _logger.LogInformation("OpenAi retrieved the date '{possibleDate}' for '{subject}' but it is in the past or very close by. Will not store it", result.StartDate, subject);
                        return null;
                    }
                    return result;
                }
                else
                {
                    _logger.LogError("OpenAI failed for detecting content with code '{Code}': '{Message}'", detectDateResult?.Error?.Code, detectDateResult?.Error?.Message);
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