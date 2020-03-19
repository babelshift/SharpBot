using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotCommandFunctions
{
    public static class SteamCommandFunction
    {
        [FunctionName("steam")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request, 
            [DurableClient] IDurableClient client, 
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var commandBody = JsonConvert.DeserializeObject<SteamCommandBody>(requestBody);
            SteamCommandProcessor processor = new SteamCommandProcessor(commandBody.SteamWebApiKey);

            var appList = await GetOrSetAppListAsync(client, processor);
            var responseMessage = await processor.ProcessCommandAsync(commandBody.Command, appList);

            return new OkObjectResult(responseMessage);
        }

        private static async Task<IDictionary<uint, string>> GetOrSetAppListAsync(IDurableClient client, SteamCommandProcessor processor)
        {
            var entityId = new EntityId(nameof(SteamAppList), "myAppList");
            var entityResponse = await client.ReadEntityStateAsync<SteamAppList>(entityId);

            IDictionary<uint, string> appList = new Dictionary<uint, string>();

            if(entityResponse.EntityExists)
            {
                appList = entityResponse.EntityState.CurrentList;
            }
            else
            {
                appList = await processor.GetSteamAppListAsync();
                await client.SignalEntityAsync<ISteamAppList>(entityId, proxy => proxy.Set(appList));
            }

            return appList;
        }
    }
}