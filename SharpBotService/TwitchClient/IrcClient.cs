using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpBotService.TwitchClient
{
    public class IrcClient : IIrcClient, IDisposable
    {
        private readonly ILogger<IrcClient> logger;

        private bool disposed = false;

        private readonly string channel;
        private readonly int port;
        private readonly string hostname;
        private readonly string userName;
        private readonly string password;

        private TcpClient tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;

        public IrcClient(string hostname, int port, string userName, string password, string channel, ILogger<IrcClient> logger)
        {
            this.logger = logger;
            this.hostname = hostname;
            this.port = port;
            this.password = password;
            this.userName = userName;
            this.channel = channel;
            tcpClient = new TcpClient();
        }

        public async Task ConnectAsync()
        {
            await tcpClient.ConnectAsync(hostname, port);
            inputStream = new StreamReader(tcpClient.GetStream());
            outputStream = new StreamWriter(tcpClient.GetStream());
            await outputStream.WriteLineAsync($"PASS {password}");
            await outputStream.WriteLineAsync($"NICK {userName}");
            await outputStream.WriteLineAsync($"USER {userName} 8 * :{userName}");
            await outputStream.WriteLineAsync($"JOIN #{channel}");
            await outputStream.FlushAsync();

            logger.LogInformation("Connected to chat server");
        }

        public void Disconnect()
        {
            tcpClient.Close();
            inputStream.Close();
            outputStream.Close();
        }

        public async Task ReconnectAsync()
        {
            Disconnect();
            await ConnectAsync();
        }

        public async Task SendIrcMessageAsync(string message)
        {
            if(!tcpClient.Connected)
            {
                logger.LogWarning($"Can't send message: '{message}' because client is not connected.");
                return;
            }

            await outputStream.WriteLineAsync(message);
            await outputStream.FlushAsync();
        }

        public async Task<string> ReadMessageAsync()
        {
            return await inputStream.ReadLineAsync();
        }

        public async Task SendChatMessageAsync(string message)
        {
            await SendIrcMessageAsync($":{userName}!{userName}@{userName}.tmi.twitch.tv PRIVMSG #{channel} :{message}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Disconnect();
            }

            disposed = true;
        }
    }
}