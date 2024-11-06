using AiMailScanner;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

internal class Program
{
    private static Config _config;
    private static MailKit.UniqueId? _lastReadId = null;

    private static async Task Main(string[] args)
    {
        _config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText("secrets.json")) ?? throw new Exception("Cannot read config");
        var services = new ServiceCollection();
        ConfigureServices(services);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var imapReceiver = serviceProvider.GetService<ImapReceiver>() ?? throw new Exception("Failed building services");
        while (true)
        {
            _lastReadId = await imapReceiver.ReceiveUnCheckedEmails(_lastReadId);
            Thread.Sleep(new TimeSpan(0, 10, 0));
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddScoped<OpenAiMailFunctions>();
        services.AddScoped<DavSender>();
        services.AddScoped<ImapReceiver>();
        IServiceCollection serviceCollection = services.AddSingleton<Config>(_config);
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            builder.AddNLog("nlog.config");
        });
    }
}