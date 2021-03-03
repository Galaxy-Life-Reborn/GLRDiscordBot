using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Services.Commands;
using Discord;
using Discord.Commands;
using Humanizer;

namespace AdvancedBot.Core.Commands.Modules
{
    [Group("counter")]
    [RequireCustomPermission(GuildPermission.ManageChannels)]
    public class CounterChannelsModule : TopModule
    {
        private ChannelCounterService _counter;

        public CounterChannelsModule(ChannelCounterService counter)
        {
            _counter = counter;
        }

        [Command("list")]
        [Summary("Lists all possible counters as of now.")]
        public async Task ListAllCountersAsync()
        {
            var possibleCounters = _counter.GetAllChannelCounters();

            var embed = new EmbedBuilder()
            .WithTitle("Possible Counters:")
            .WithColor(Color.DarkGreen)
            .WithFooter("Create one by executing !counter create <channel-id> <counter>");

            /* i=1 to skip None */
            for (int i = 1; i < possibleCounters.Length; i++)
            {
                embed.AddField(possibleCounters[i].Trigger.Humanize(), $"{possibleCounters[i].Description}\n\u200b");
            }

            await ReplyAsync("", false, embed.Build());
        }
        
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [Command("setup")]
        [Summary("Sets up one counter of each type.")]
        public async Task SetupCountersAsync()
        {
            var possibleCounters = _counter.GetAllChannelCounters();

            /* i=1 to skip None */
            for (int i = 1; i < possibleCounters.Length; i++)
            {
                var c = possibleCounters[i];
                var voiceChannel = await Context.Guild.CreateVoiceChannelAsync($"{c.Trigger} counter");
                await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(connect: PermValue.Deny));

                try
                {
                    _counter.AddNewChannelCounter(Context.Guild.Id, new ChannelCounter(voiceChannel.Id, possibleCounters[i].Type));
                }
                catch (Exception)
                {
                    await voiceChannel.DeleteAsync();
                }
            }

            await ReplyAsync("Successfully set up one of each counter." +
            "\nPlease wait up to **3 minutes** for the counters to set up." +
            "\n\n**TIP:** You can delete the voice channels of counters you dont want.");
        }

        [RequireBotPermission(GuildPermission.ManageChannels)]
        [Command("create")][Alias("add")]
        [Summary("Adds a counter to an existing voice channel")]
        public async Task CreateNewCounterAsync([EnsureSameGuild] IVoiceChannel channel, [Remainder] string input)
        {
            var type = _counter.ParseChannelCounterTypeFromInput(input);

            _counter.AddNewChannelCounter(channel.Guild.Id, new ChannelCounter(channel.Id, type));
            await ReplyAsync($"Successfully added the **{type.Humanize()}** to the channel, it will update within 3 minutes");
        }

        [Command("remove")][Alias("delete", "destroy")]
        [Summary("Removes an existing counter.")]
        public async Task RemoveCounterAsync([Remainder]string input)
        {
            var type = _counter.ParseChannelCounterTypeFromInput(input);

            _counter.RemoveChannelCounterByType(Context.Guild.Id, type);
            await ReplyAsync($"Successfully removed the **{type.Humanize()}** from the channel, it will no longer update.");
        }

        [Command("remove")][Alias("delete", "destroy")]
        [Summary("Removes an existing counter.")]
        public async Task RemoveCounterAsync([EnsureSameGuild] IVoiceChannel vc)
        {
            _counter.RemoveChannelCounterByChannel(Context.Guild.Id, vc.Id);
            await ReplyAsync($"Successfully removed the counter from **{vc.Id}**, it will no longer update.");
        }
    }
}
