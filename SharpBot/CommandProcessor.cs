using SharpBot.Processors;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharpBot
{
    public class CommandProcessor
    {
        private readonly IrcClient client;
        private readonly string steamWebApiKey;

        public CommandProcessor(string steamWebApiKey, IrcClient client)
        {
            this.steamWebApiKey = steamWebApiKey;
            this.client = client;
        }

        public async Task ProcessAsync(string message)
        {
            message = message.Trim().ToLower();
            var mainCommand = message.Split(' ')[0];

            Dictionary<string, IProcessor> processors = new Dictionary<string, IProcessor>();
            processors.Add("!help", new HelpCommandProcessor());
            processors.Add("!steam", new SteamCommandProcessor(steamWebApiKey));

            if (processors.ContainsKey(mainCommand))
            {
                var processor = processors[mainCommand];
                var response = await processor.ProcessCommandAsync(message);
                client.SendChatMessage(response);
            }
        }
    }
}