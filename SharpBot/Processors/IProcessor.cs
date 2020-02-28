using System.Threading.Tasks;

namespace SharpBot
{
    public interface IProcessor
    {
        public Task<string> ProcessCommandAsync(string inputMessage);
    }
}