using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    var commandProcessorSettings = new CommandProcessorSettings()
                    {
                        Url = hostContext.Configuration["AzureFunction:Url"]
                    };
                    var steamCommandProcessorSettings = new SteamCommandProcessorSettings()
                    {
                        SteamWebApiKey = hostContext.Configuration["Secrets:SteamWebApiKey"]
                    };
                    var azureFunctionClient = new AzureFunctionClient(commandProcessorSettings, steamCommandProcessorSettings);

                    services.AddSingleton<IIrcClient>(x =>
                    {
                        return new IrcClient(
                            hostContext.Configuration["Secrets:TwitchIrcUrl"],
                            int.Parse(hostContext.Configuration["Secrets:TwitchIrcPort"]),
                            hostContext.Configuration["Secrets:BotUsername"],
                            hostContext.Configuration["Secrets:TwitchOAuthToken"],
                            hostContext.Configuration["Secrets:ChannelName"],
                            x.GetService<ILogger<IrcClient>>());
                    });
                    services.AddSingleton<IPinger>(x =>
                    {
                        return new Pinger(x.GetService<IIrcClient>(), x.GetService<ILogger<Pinger>>());
                    });
                    services.AddSingleton<ICommandProcessorSettings>(commandProcessorSettings);
                    services.AddSingleton<ISteamCommandProcessorSettings>(steamCommandProcessorSettings);
                    services.AddSingleton<IAzureFunctionClient>(azureFunctionClient);
                    services.AddHostedService<TwitchBotWorker>();
                });
    }
}