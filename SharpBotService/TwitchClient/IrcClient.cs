using System;
using System.IO;
using System.Net.Sockets;

namespace SharpBotService.TwitchClient
{
    public class IrcClient : IIrcClient, IDisposable
    {
        private bool disposed = false;

        private readonly string channel;
        private readonly int port;
        private readonly string hostname;
        private readonly string userName;
        private readonly string password;

        private TcpClient tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;

        public IrcClient(string hostname, int port, string userName, string password, string channel)
        {
            this.hostname = hostname;
            this.port = port;
            this.password = password;
            this.userName = userName;
            this.channel = channel;
        }

        public void Connect()
        {
            tcpClient = new TcpClient(hostname, port);
            inputStream = new StreamReader(tcpClient.GetStream());
            outputStream = new StreamWriter(tcpClient.GetStream());

            outputStream.WriteLine($"PASS {password}");
            outputStream.WriteLine($"NICK {userName}");
            outputStream.WriteLine($"USER {userName} 8 * :{userName}");
            outputStream.WriteLine($"JOIN #{channel}");
            outputStream.Flush();
        }

        public void Disconnect()
        {
            tcpClient.Close();
            inputStream.Close();
            outputStream.Close();
        }

        public void SendIrcMessage(string message)
        {
            outputStream.WriteLine(message);
            outputStream.Flush();
        }

        public string ReadMessage()
        {
            return inputStream.ReadLine();
        }

        public void SendChatMessage(string message)
        {
            SendIrcMessage($":{userName}!{userName}@{userName}.tmi.twitch.tv PRIVMSG #{channel} :{message}");
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
                tcpClient.Close();
                inputStream.Close();
                outputStream.Close();
            }

            disposed = true;
        }
    }
}