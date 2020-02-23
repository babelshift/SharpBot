using System;

namespace SharpBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            IrcClientProperties props = new IrcClientProperties();

            using (var client = new IrcClient("irc.twitch.tv", 6667, props.BotUsername, props.OAuthToken, props.ChannelName))
            {
                // this will send pings, but do we also need to reply to pings from twitch per documentation?
                Pinger pinger = new Pinger(client);
                pinger.Start();

                // listen for commands forever
                while (true)
                {
                    Console.WriteLine("Reading message");
                    var message = client.ReadMessage();
                    Console.WriteLine($"Read message: {message}");

                    var parsedMessage = ParseMessage(message);
                    if (parsedMessage == "!ping")
                    {
                        client.SendChatMessage("pong");
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