using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            sender = new Thread(new ParameterizedThreadStart(Run));
        }

        public void Start(CancellationToken ct)
        {
            sender.IsBackground = true;
            sender.Start(ct);
        }

        private void Run(object ct)
        {
            try
            {
                CancellationToken t = (CancellationToken)ct;
                while (!t.IsCancellationRequested)
                {
                    logger.LogInformation("Sending PING");
                    client.SendIrcMessageAsync("PING irc.twitch.tv");
                    logger.LogInformation("Sent PING");
                    Task.Delay(TimeSpan.FromMinutes(5), t).Wait();
                }
            }
            catch (TaskCanceledException ex)
            {
                logger.LogWarning(ex, "Ending thread because task was cancelled.");
            }
        }
    }
}