using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpBotService.TwitchClient;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBotService
{
    public class TwitchBotWorker : BackgroundService
    {
        private readonly ILogger<TwitchBotWorker> _logger;
        private readonly IIrcClient _ircClient;
        private readonly IPinger _pinger;

        public TwitchBotWorker(ILogger<TwitchBotWorker> logger, IIrcClient ircClient, IPinger pinger)
        {
            _logger = logger;
            _ircClient = ircClient;
            _pinger = pinger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _ircClient.Connect();
                _pinger.Start();

                // Listen for commands and process them forever
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Waiting for next message from chat server");
                    var message = _ircClient.ReadMessage();
                    _logger.LogInformation($"Received message: {message}");

                    if (message.Contains("PRIVMSG"))
                    {
                        if (message.Contains("hello"))
                        {
                            _ircClient.SendChatMessage("Hello World");
                        }
                        // TODO: Hook up to call Azure Function
                        //var parsedMessage = ParseMessage(message);
                        //await commandProcessor.ProcessAsync(parsedMessage);
                    }
                    else if (message.StartsWith("PING"))
                    {
                        _ircClient.SendIrcMessage("PONG :tmi.twitch.tv");
                    }
                }
            }
            finally
            {
                _ircClient.Disconnect();
            }
        }
    }
}