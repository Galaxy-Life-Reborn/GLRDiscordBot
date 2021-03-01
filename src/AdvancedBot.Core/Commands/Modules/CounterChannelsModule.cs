using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
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
            await ReplyAsync("`flash`, `pa`");
        }
        
        [Command("setup")]
        [Summary("Sets up one counter of each type.")]
        public async Task SetupCountersAsync()
        {
            var flashChannel = await Context.Guild.CreateVoiceChannelAsync("Flash Status: Online");
            var paChannel = await Context.Guild.CreateVoiceChannelAsync("PA Status: Online");

            _counter.TryAddNewChannelCounter(Context.Guild.Id, new ChannelCounter(paChannel.Id, ChannelCounterType.PAStatus));
            _counter.TryAddNewChannelCounter(Context.Guild.Id, new ChannelCounter(flashChannel.Id, ChannelCounterType.FlashStatus));

            await ReplyAsync("Successfully set up all voice channels");
        }

        [Command("create")][Alias("add")]
        [Summary("Adds a counter to an existing voice channel")]
        public async Task CreateNewCounterAsync([EnsureSameGuild] IVoiceChannel channel, string input)
        {
            var type = _counter.ParseChannelCounterTypeFromInput(input);

            if (_counter.TryAddNewChannelCounter(channel.Guild.Id, new ChannelCounter(channel.Id, type)))
            {
                await ReplyAsync($"Successfully added the **{type.Humanize()}** to the channel, it will update within 3 minutes");
            }
            else
            {
                throw new Exception($"Could not create the channel counter. Make sure it doesn't already exist.");
            }
        }

        [Command("remove")][Alias("delete", "destroy")]
        [Summary("Removes an existing counter.")]
        public async Task RemoveCounterAsync(string input)
        {
            var type = _counter.ParseChannelCounterTypeFromInput(input);

            if (_counter.TryRemoveChannelCounterByType(Context.Guild.Id, type))
            {
                await ReplyAsync($"Successfully removed the **{type.Humanize()}** from the channel, it will no longer update.");
            }
            else
            {
                throw new Exception($"Could not remove the counter associated with this type.");
            }
        }

        [Command("remove")][Alias("delete", "destroy")]
        [Summary("Removes an existing counter.")]
        public async Task RemoveCounterAsync(IVoiceChannel vc)
        {
            if (_counter.TryRemoveChannelCounterByChannel(Context.Guild.Id, vc.Id))
            {
                await ReplyAsync($"Successfully the counter from **{vc.Id}**, it will no longer update.");
            }
            else
            {
                throw new Exception($"Could not remove the counter associated with this channel.");
            }
        }
    }
}
