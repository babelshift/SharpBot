using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharpBotService.FunctionConsumer;
using SharpBotService.TwitchClient;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
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

        private readonly TimeSpan reconnectWaitTime = TimeSpan.FromSeconds(5);
        private int maxReconnectAttempts = 5;

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
                await _ircClient.ConnectAsync();
                _pinger.Start(stoppingToken);

                // Listen for commands and process them forever
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = await _ircClient.ReadMessageAsync();
                        await HandleReceivedMessageAsync(message);
                    }
                    catch (IOException ioex)
                    {
                        _logger.LogError(ioex, "The following IO exception occurred as a catch-all during command processing.");
                        await HandleReconnectAttemptsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "The following exception occurred as a catch-all during command processing.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The following exception occurred as a catch-all during ExecuteAsync.");
            }
            finally
            {
                _ircClient.Disconnect();
            }
        }

        private async Task HandleReconnectAttemptsAsync(CancellationToken stoppingToken)
        {
            for (int reconnectAttempt = 0; reconnectAttempt < maxReconnectAttempts; reconnectAttempt++)
            {
                _logger.LogInformation($"Reconnect attempt {reconnectAttempt} of {maxReconnectAttempts}.");
                try
                {
                    await _ircClient.ReconnectAsync();
                }
                catch (SocketException se)
                {
                    _logger.LogError(se, "The following socket exception occurred while attetmping a reconnect.");
                    await Task.Delay(reconnectWaitTime, stoppingToken);
                }
            }
        }

        private async Task HandleReceivedMessageAsync(string message)
        {
            _logger.LogInformation($"Received message: {message}");

            if (message.Contains("PRIVMSG"))
            {
                var parsedMessage = ParseMessage(message);
                var response = await _azureFunctionClient.ProcessCommandAsync(parsedMessage);
                await _ircClient.SendChatMessageAsync(response);
            }
            else if (message.StartsWith("PING"))
            {
                await _ircClient.SendIrcMessageAsync("PONG :tmi.twitch.tv");
                _logger.LogInformation("Sent PONG");
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