using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace SharpBotService.TwitchClient
{
    public class Pinger : IPinger
    {
        private readonly ILogger<Pinger> logger;
        private readonly IIrcClient client;
        private readonly Thread sender;

        public Pinger(IIrcClient client, ILogger<Pinger> logger)
        {
            this.logger = logger;
            this.client = client;
            sender = new Thread(new ThreadStart(Run));
        }

        public void Start()
        {
            sender.IsBackground = true;
            sender.Start();
            logger.LogInformation("Started pinger process");
        }

        private void Run()
        {
            while (true)
            {
                logger.LogInformation("Sending PING");
                client.SendIrcMessage("PING irc.twitch.tv");
                Thread.Sleep(TimeSpan.FromMinutes(5));
                logger.LogInformation("Sent PING");
            }
        }
    }
}