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
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request, 
            [DurableClient] IDurableClient client, 
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Extract input from the POST body
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var commandBody = JsonConvert.DeserializeObject<SteamCommandBody>(requestBody);
            SteamCommandProcessor processor = new SteamCommandProcessor(commandBody.SteamWebApiKey);

            var appList = await GetOrSetAppListAsync(client, processor);
            var responseMessage = await processor.ProcessCommandAsync(commandBody.Command, appList);

            return new OkObjectResult(responseMessage);
        }

        /// <summary>
        /// Gets or sets the Steam App List by using Azure Durable Entities. These entities are stored in Azure Blob Storage for resiliency
        /// and re-use. Without these entities, we would need to re-query for this data evey time a command came in.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="processor"></param>
        /// <returns></returns>
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