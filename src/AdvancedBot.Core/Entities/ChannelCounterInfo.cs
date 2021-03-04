using AdvancedBot.Core.Entities.Enums;
using Humanizer;

namespace AdvancedBot.Core.Entities
{
    public class ChannelCounterInfo
    {
        public ChannelCounterInfo(ChannelCounterType type)
        {
            Type = type;
            Trigger = type.Humanize().ToLower();
            CheckIntervalInMinutes = 6;
            Description = "No description provided.";

            switch (type)
            {
                case ChannelCounterType.FlashStatus:
                    Trigger = "flash";
                    Description = "Shows the current server status of flash servers.";
                    CheckIntervalInMinutes = 3;
                    break;
                case ChannelCounterType.PAStatus:
                    Trigger = "pa";
                    Description = "Shows the current server status of pa servers.";
                    CheckIntervalInMinutes = 3;
                    break;
                case ChannelCounterType.OnlinePlayers:
                    Description = "Shows the current amount of in-game flash players.";
                    break;
                case ChannelCounterType.TotalCommandsExecuted:
                    Trigger = "commands";
                    Description = "Shows the current amount of executed commands.";
                    break;
                case ChannelCounterType.MemberCount:
                    Description = "Shows the current amount of all Discord Users in this server.";
                    break;
                default:
                    break;
            }
        }

        public ChannelCounterType Type { get; }
        public string Trigger { get; }
        public string Description { get; }
        public int CheckIntervalInMinutes { get; }
    }
}
