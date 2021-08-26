using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands.Preconditions
{
    public class RequireCustomPermission : PreconditionAttribute
    {
        private GuildPermission _permission;

        public RequireCustomPermission(GuildPermission permission = default)
        {
            _permission = permission;
        }
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guildUser = context.User as SocketGuildUser;

            if (guildUser.GuildPermissions.Has(_permission) || guildUser.GuildPermissions.Has(GuildPermission.Administrator))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("Insufficient permissions."));
        }
    }
}
