using System.Threading;

namespace SharpBotService.TwitchClient
{
    public interface IPinger
    {
        void Start(CancellationToken ct);
    }
}