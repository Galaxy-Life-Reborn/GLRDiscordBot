using System;
using System.Linq;
using System.Net.Http;
using System.Timers;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord.WebSocket;
using GLR.Net;

namespace AdvancedBot.Core.Services.Commands
{
    public class ChannelCounterService
    {
        private DiscordSocketClient _client;
        private GLRClient _glr;
        private GuildAccountService _guild;
        private Timer _timer = new Timer(3 * 10 * 1000);

        public ChannelCounterService(DiscordSocketClient client, GLRClient glr, GuildAccountService guild)
        {
            _client = client;
            _glr = glr;
            _guild = guild;

            _timer.Start();
            _timer.Elapsed += OnTimerElapsed;
        }

        private void OnTimerElapsed(object timerObj, ElapsedEventArgs e)
        {
            _timer.Stop();

            HandleActiveChannelCounters();

            _timer.Start();
        }

        public ChannelCounterType ParseChannelCounterTypeFromInput(string input)
        {
            ChannelCounterType type;

            type = input.ToLower() == "flash" ? ChannelCounterType.FlashStatus
            : input.ToLower() == "pa" ? ChannelCounterType.PAStatus : ChannelCounterType.None;

            return type;
        }

        public bool TryAddNewChannelCounter(ulong guildId, ChannelCounter counter)
        {
            var guild = _guild.GetOrCreateGuildAccount(guildId);
            var fCounter = guild.ChannelCounters.Find(x => x.Type == counter.Type);
            var cCounter = guild.ChannelCounters.Find(x => x.ChannelId == counter.ChannelId);

            if (fCounter != null)
                return false;

            guild.ChannelCounters.Add(counter);
            _guild.SaveGuildAccount(guild);
            return true;
        }

        public bool TryRemoveChannelCounterByType(ulong guildId, ChannelCounterType counterType)
        {
            var guild = _guild.GetOrCreateGuildAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.Type == counterType);

            if (counter is null)
                return false;
            
            guild.ChannelCounters.Remove(counter);

            _guild.SaveGuildAccount(guild);
            return true;
        }

        public bool TryRemoveChannelCounterByChannel(ulong guildId, ulong channelId)
        {
            var guild = _guild.GetOrCreateGuildAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.ChannelId == channelId);

            if (counter is null)
                return false;
            
            guild.ChannelCounters.Remove(counter);

            _guild.SaveGuildAccount(guild);
            return true;
        }

        private void HandleActiveChannelCounters()
        {
            var guilds = _guild.GetAllGuilds();
            string flashStatus = GetFlashStatus();
            string paStatus = GetPaStatus();

            for (int i = 0; i < guilds.Length; i++)
            {                
                var guild = _client.GetGuild(guilds[i].Id);

                if (!guilds[i].ChannelCounters.Any())
                    continue;

                for (int j = 0; j < guilds[i].ChannelCounters.ToArray().Length; j++)
                {
                    var channel = guild.GetChannel(guilds[i].ChannelCounters[j].ChannelId);

                    if (channel is null)
                    {
                        /* Channel got removed */
                        guilds[i].ChannelCounters.Remove(guilds[i].ChannelCounters[j]);
                        _guild.SaveGuildAccount(guilds[i]);
                        continue;
                    }

                    switch (guilds[i].ChannelCounters[j].Type)
                    {
                        case ChannelCounterType.FlashStatus:
                            string newName = $"Flash Status: {flashStatus}";
                            if (channel.Name != newName)
                                channel.ModifyAsync(x => x.Name = newName);
                            break;
                        case ChannelCounterType.PAStatus:
                            string newChannelName = $"PA Status: {paStatus}";
                            if (channel.Name != newChannelName)
                                channel.ModifyAsync(x => x.Name = newChannelName);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private string GetFlashStatus()
        {
            var newStatus = "Offline";

            try
            {
                var status = _glr.GetServerStatus().GetAwaiter().GetResult();
                if (status.Ready)
                    newStatus = "Online";
                else newStatus = "Launching";
            }
            catch (Exception exc)
            {
                newStatus = "Offline";
            }

            return newStatus;
        }
    
        private string GetPaStatus()
        {
            var newStatus = "Offline";

            try
            {
                var test = new HttpClient().GetAsync("http://pa.galaxylifereborn.com/star/status").GetAwaiter().GetResult();
                newStatus = "Online";
            }
            catch (Exception exc)
            {
                newStatus = "Offline";
            }

            return newStatus;
        }
    }
}
