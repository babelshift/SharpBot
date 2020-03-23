using System.Threading.Tasks;

namespace SharpBotService.FunctionConsumer
{
    public interface IAzureFunctionClient
    {
        Task<string> ProcessCommandAsync(string parsedMessage);
    }
}