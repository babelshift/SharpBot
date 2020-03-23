using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharpBotService.FunctionConsumer
{
    public class AzureFunctionClient : IAzureFunctionClient
    {
        private readonly ICommandProcessorSettings _commandProcessorSettings;
        private readonly ISteamCommandProcessorSettings _steamCommandProcessorSettings;

        public AzureFunctionClient(
            ICommandProcessorSettings commandProcessorSettings,
            ISteamCommandProcessorSettings steamCommandProcessorSettings)
        {
            _commandProcessorSettings = commandProcessorSettings;
            _steamCommandProcessorSettings = steamCommandProcessorSettings;
        }

        public async Task<string> ProcessCommandAsync(string parsedMessage)
        {
            var matches = Regex.Match(parsedMessage, @"^!\w+");
            if (matches.Success)
            {
                var match = matches.Groups[0].Value;
                match = match.Replace("!", string.Empty);
                var url = _commandProcessorSettings.Url.Replace("{command}", match);

                HttpClient httpClient = new HttpClient();
                var body = new SteamCommandBody()
                {
                    Command = parsedMessage,
                    SteamWebApiKey = _steamCommandProcessorSettings.SteamWebApiKey
                };
                StringContent requestContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, requestContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            return string.Empty;
        }
    }
}
