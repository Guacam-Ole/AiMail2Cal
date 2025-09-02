using System.Reflection;
using AiMailScanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

internal class Program
{
    private static uint? _lastReadId = null;

    private static async Task Main()
    {
        Console.WriteLine("Application just Started");
        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText("secrets.json")) ??
                     throw new Exception("Cannot read config");
        var services = new ServiceCollection();
        ConfigureServices(services, config);

        await using var serviceProvider = services.BuildServiceProvider();
        var imapReceiver = serviceProvider.GetService<ImapReceiver>() ??
                           throw new Exception("Failed building services");
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
        services.AddSingleton(config);
        services.AddLogging(cfg => cfg.SetMinimumLevel(LogLevel.Debug));
        services.AddSerilog(cfg =>
        {
            cfg.MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("job", Assembly.GetEntryAssembly()?.GetName().Name)
                .Enrich.WithProperty("desktop", Environment.GetEnvironmentVariable("DESKTOP_SESSION"))
                .Enrich.WithProperty("language", Environment.GetEnvironmentVariable("LANGUAGE"))
                .Enrich.WithProperty("lc", Environment.GetEnvironmentVariable("LC_NAME"))
                .Enrich.WithProperty("timezone", Environment.GetEnvironmentVariable("TZ"))
                .Enrich.WithProperty("dotnetVersion", Environment.GetEnvironmentVariable("DOTNET_VERSION"))
                .Enrich.WithProperty("inContainer", Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"))
                .WriteTo.GrafanaLoki(Environment.GetEnvironmentVariable("LOKIURL") ?? "http://thebeast:3100",
                    propertiesAsLabels: ["job"]);
            if (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ==
                "Debug")
            {
                cfg.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
        });
    }
}