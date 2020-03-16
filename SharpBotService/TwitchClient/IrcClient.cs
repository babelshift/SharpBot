using System;
using System.IO;
using System.Net.Sockets;

namespace SharpBotService.TwitchClient
{
    public class IrcClient : IDisposable
    {
        private bool disposed = false;
        private string userName;
        private string channel;

        private TcpClient tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;

        public IrcClient(string hostname, int port, string userName, string password, string channel)
        {
            this.userName = userName;
            this.channel = channel;

            tcpClient = new TcpClient(hostname, port);
            inputStream = new StreamReader(tcpClient.GetStream());
            outputStream = new StreamWriter(tcpClient.GetStream());

            outputStream.WriteLine($"PASS {password}");
            outputStream.WriteLine($"NICK {userName}");
            outputStream.WriteLine($"USER {userName} 8 * :{userName}");
            outputStream.WriteLine($"JOIN #{channel}");
            outputStream.Flush();
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