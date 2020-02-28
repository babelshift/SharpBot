using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharpBot.Processors
{
    public class HelpCommandProcessor : IProcessor
    {
        public async Task<string> ProcessCommandAsync(string inputMessage)
        {
            return $"!steam to integrate with Steam";
        }
    }
}
