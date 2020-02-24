using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace SharpBot
{
    internal class Program
    {
        private static IrcClientProperties props;

        private static async Task Main(string[] args)
        {
            // Lookup app config
            var config = await File.ReadAllTextAsync("appsettings.json");
            props = JsonConvert.DeserializeObject<IrcClientProperties>(config);

            // Bootstrap the IRC client
            using (var client = new IrcClient(props.TwitchIrcUrl, props.TwitchIrcPort, props.BotUsername, props.TwitchOAuthToken, props.ChannelName))
            {
                // this will send pings, but do we also need to reply to pings from twitch per documentation?
                var pinger = new Pinger(client);
                pinger.Start();

                var processor = new CommandProcessor(props.SteamWebApiKey, client);

                // Listen for commands forever
                while (true)
                {
                    Console.WriteLine("Reading message");
                    var message = client.ReadMessage();
                    Console.WriteLine($"Message: {message}");

                    var parsedMessage = ParseMessage(message);
                    await processor.ProcessAsync(parsedMessage);
                }
            }
        }

        private static string ParseMessage(string message)
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