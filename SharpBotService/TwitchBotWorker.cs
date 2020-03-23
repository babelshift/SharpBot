using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharpBotService.FunctionConsumer;
using SharpBotService.TwitchClient;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBotService
{
    public class TwitchBotWorker : BackgroundService
    {
        private readonly ILogger<TwitchBotWorker> _logger;
        private readonly IIrcClient _ircClient;
        private readonly IPinger _pinger;
        private readonly IAzureFunctionClient _azureFunctionClient;

        public TwitchBotWorker(ILogger<TwitchBotWorker> logger, 
            IIrcClient ircClient, 
            IPinger pinger,
            IAzureFunctionClient azureFunctionClient)
        {
            _logger = logger;
            _ircClient = ircClient;
            _pinger = pinger;
            _azureFunctionClient = azureFunctionClient;
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
                        var parsedMessage = ParseMessage(message);
                        var response = await _azureFunctionClient.ProcessCommandAsync(parsedMessage);
                        _ircClient.SendChatMessage(response);
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

        private string ParseMessage(string message)
        {
            string[] splitMessage = message.Split(':');
            if (splitMessage.Length == 3)
            {
                return splitMessage[2];
            }

            return string.Empty;
        }
    }
}