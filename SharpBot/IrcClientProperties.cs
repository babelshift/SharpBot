using System;
using System.Collections.Generic;
using System.Text;

namespace SharpBot
{
    public class IrcClientProperties
    {
        public string BotUsername { get; set; }
        public string ChannelName { get; set; }
        public string TwitchOAuthToken { get; set; }
        public string SteamWebApiKey { get; set; }
        public string SteamWebApiBaseUrl { get; set; }
        public string TwitchIrcUrl { get; set; }
        public int TwitchIrcPort { get; set; }
    }
}
