namespace SharpBotService.TwitchClient
{
    public interface IIrcClient
    {
        void Connect();
        void Disconnect();
        void SendIrcMessage(string message);
        string ReadMessage();
        void SendChatMessage(string message);
    }
}