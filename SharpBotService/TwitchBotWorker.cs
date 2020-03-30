using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharpBotService.FunctionConsumer;
using SharpBotService.TwitchClient;
using System;
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The following exception occurred as a catch-all during ExecuteAsync.");
            }
            finally
            {
                _ircClient.Disconnect();
            }

            // Listen for commands and process them forever
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = _ircClient.ReadMessage();

                    if (message.Contains("PRIVMSG"))
                    {
                        var parsedMessage = ParseMessage(message);
                        var response = await _azureFunctionClient.ProcessCommandAsync(parsedMessage);
                        _ircClient.SendChatMessage(response);
                    }
                    else if (message.StartsWith("PING"))
                    {
                        _logger.LogInformation("Received PING");
                        _ircClient.SendIrcMessage("PONG :tmi.twitch.tv");
                        _logger.LogInformation("Sent PONG");
                    }
                    else
                    {
                        _logger.LogInformation($"Received message: {message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "The following exception occurred as a catch-all during command processing.");
                }
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