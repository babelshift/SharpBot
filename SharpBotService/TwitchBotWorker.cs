using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharpBotService.TwitchClient;

namespace SharpBotService
{
    public class TwitchBotWorker : BackgroundService
    {
        private readonly ILogger<TwitchBotWorker> _logger;
        private readonly IConfiguration _configuration;

        public TwitchBotWorker(ILogger<TwitchBotWorker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var client = new IrcClient(
                _configuration["Secrets:TwitchIrcUrl"],
                int.Parse(_configuration["Secrets:TwitchIrcPort"]),
                _configuration["Secrets:BotUsername"],
                _configuration["Secrets:TwitchOAuthToken"],
                _configuration["Secrets:ChannelName"]))
            {
                // start the pinger
                var pinger = new Pinger(client);
                pinger.Start();

                // Listen for commands and process them forever
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Waiting for next message from chat server");
                    var message = client.ReadMessage();
                    _logger.LogInformation($"Received message: {message}");

                    if (message.Contains("PRIVMSG"))
                    {
                        if (message.Contains("hello"))
                        {
                            client.SendChatMessage("Hello World");
                        }
                        //var parsedMessage = ParseMessage(message);
                        //await commandProcessor.ProcessAsync(parsedMessage);
                    }
                    else if (message.StartsWith("PING"))
                    {
                        client.SendIrcMessage("PONG :tmi.twitch.tv");
                    }
                }
            }
        }
    }
}
