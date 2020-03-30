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
                        var ircUrl = hostContext.Configuration["Secrets:TwitchIrcUrl"];
                        var port = int.Parse(hostContext.Configuration["Secrets:TwitchIrcPort"]);
                        var botUsername = hostContext.Configuration["Secrets:BotUsername"];
                        var twitchOAuthToken = hostContext.Configuration["Secrets:TwitchOAuthToken"];
                        var channelName = hostContext.Configuration["Secrets:ChannelName"];
                        var logger = x.GetService<ILogger<IrcClient>>();

                        return new IrcClient(ircUrl, port, botUsername, twitchOAuthToken, channelName, logger);
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