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
            var config = await File.ReadAllTextAsync("appsettings.json");
            props = JsonConvert.DeserializeObject<IrcClientProperties>(config);

            using (var client = new IrcClient(props.TwitchIrcUrl, props.TwitchIrcPort, props.BotUsername, props.TwitchOAuthToken, props.ChannelName))
            {
                var pinger = new Pinger(client);
                pinger.Start();

                var processor = new CommandProcessor(props.SteamWebApiKey, client);

                // Listen for commands and process them forever
                while (true)
                {
                    Console.WriteLine("Waiting for next message from chat server");
                    var message = client.ReadMessage();
                    Console.WriteLine($"Received message: {message}");

                    if (message.Contains("PRIVMSG"))
                    {
                        var parsedMessage = ParseMessage(message);
                        await processor.ProcessAsync(parsedMessage);
                    }
                    else if(message.StartsWith("PING"))
                    {
                        client.SendIrcMessage("PONG :tmi.twitch.tv");
                    }
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