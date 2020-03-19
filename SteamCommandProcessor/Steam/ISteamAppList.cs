using System.Collections.Generic;

namespace BotCommandFunctions
{
    public interface ISteamAppList
    {
        void Set(IDictionary<uint, string> list);
    }
}