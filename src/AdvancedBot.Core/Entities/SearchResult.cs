using Discord.Commands;

namespace AdvancedBot.Core.Entities
{
    public class AdvancedSearchResult
    {
        public AdvancedSearchResult()
        {
            Module = null;
            Command = null;
        }

        public AdvancedSearchResult(ModuleInfo module, CommandInfo command)
        {
            Module = module;
            Command = command;
        }

        public ModuleInfo Module { get; set; }
        public CommandInfo Command { get; set; }
    }
}
