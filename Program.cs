using AiMailScanner;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

internal class Program
{
    private static uint? _lastReadId = null;

    private static async Task Main()
    {
        Console.WriteLine("Application just Started");
        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText("secrets.json")) ?? throw new Exception("Cannot read config");
        var services = new ServiceCollection();
        ConfigureServices(services, config);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var imapReceiver = serviceProvider.GetService<ImapReceiver>() ?? throw new Exception("Failed building services");
        while (true)
        {
            _lastReadId = await imapReceiver.ReceiveUnCheckedEmails(_lastReadId);
            Thread.Sleep(new TimeSpan(0, 0, 30));
        }
    }

    private static void ConfigureServices(ServiceCollection services, Config config)
    {
        services.AddScoped<OpenAiMailFunctions>();
        services.AddScoped<DavSender>();
        services.AddScoped<ImapReceiver>();
        IServiceCollection serviceCollection = services.AddSingleton(config);
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddNLog("nlog.config");
        });
    }
}