using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotCommandFunctions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SteamAppList : ISteamAppList
    {
        [JsonProperty("currentList")]
        public IDictionary<uint, string> CurrentList { get; set; }

        public void Set(IDictionary<uint, string> list) => CurrentList = list;

        [FunctionName(nameof(SteamAppList))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<SteamAppList>();
    }
}