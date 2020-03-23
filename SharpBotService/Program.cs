using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpBotService.FunctionConsumer;
using SharpBotService.TwitchClient;

namespace SharpBotService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var ircClient = new IrcClient(
                        hostContext.Configuration["Secrets:TwitchIrcUrl"],
                        int.Parse(hostContext.Configuration["Secrets:TwitchIrcPort"]),
                        hostContext.Configuration["Secrets:BotUsername"],
                        hostContext.Configuration["Secrets:TwitchOAuthToken"],
                        hostContext.Configuration["Secrets:ChannelName"]
                    );
                    var pinger = new Pinger(ircClient);
                    var commandProcessorSettings = new CommandProcessorSettings()
                    {
                        Url = hostContext.Configuration["AzureFunction:Url"]
                    };
                    var steamCommandProcessorSettings = new SteamCommandProcessorSettings()
                    {
                        SteamWebApiKey = hostContext.Configuration["Secrets:SteamWebApiKey"]
                    };
                    var azureFunctionClient = new AzureFunctionClient(commandProcessorSettings, steamCommandProcessorSettings);

                    services.AddSingleton<IIrcClient>(ircClient);
                    services.AddSingleton<IPinger>(pinger);
                    services.AddSingleton<ICommandProcessorSettings>(commandProcessorSettings);
                    services.AddSingleton<ISteamCommandProcessorSettings>(steamCommandProcessorSettings);
                    services.AddSingleton<IAzureFunctionClient>(azureFunctionClient);
                    services.AddHostedService<TwitchBotWorker>();
                });
    }
}