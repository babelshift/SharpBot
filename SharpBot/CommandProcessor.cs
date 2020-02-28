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
        private readonly Dictionary<string, IProcessor> processors = new Dictionary<string, IProcessor>();

        public CommandProcessor(string steamWebApiKey, IrcClient client)
        {
            this.steamWebApiKey = steamWebApiKey;
            this.client = client;
        }

        public async Task InitializeAsync()
        {
            processors.Add("!help", new HelpCommandProcessor());
            var steamCommandProcessor = new SteamCommandProcessor(steamWebApiKey);
            await steamCommandProcessor.InitializeAsync();
            processors.Add("!steam", steamCommandProcessor);
        }

        public async Task ProcessAsync(string message)
        {
            try
            {
                message = message.Trim().ToLower();
                var mainCommand = message.Split(' ')[0];

                if (processors.ContainsKey(mainCommand))
                {
                    var processor = processors[mainCommand];
                    var response = await processor.ProcessCommandAsync(message);
                    client.SendChatMessage(response);
                }
            }
            // we should do nothing if any exception occurs because we can't recover with bad input
            catch { }
        }
    }
}