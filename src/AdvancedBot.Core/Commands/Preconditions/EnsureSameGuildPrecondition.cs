using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands.Preconditions
{
    public class EnsureSameGuildAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            var vc = value as SocketVoiceChannel;

            if (context.Guild.Id != vc.Guild.Id)
                return Task.FromResult(PreconditionResult.FromError("Cannot edit channels outside of your own guild"));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
