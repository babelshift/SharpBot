using System.Threading.Tasks;

namespace SharpBotService.TwitchClient
{
    public interface IIrcClient
    {
        Task ConnectAsync();
        void Disconnect();
        Task ReconnectAsync();
        Task<string> ReadMessageAsync();
        Task SendIrcMessageAsync(string message);
        Task SendChatMessageAsync(string message);
    }
}