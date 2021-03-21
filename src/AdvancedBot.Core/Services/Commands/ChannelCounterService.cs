using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Timers;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord.WebSocket;
using GLR.Net;
using GLR.Net.Entities;

namespace AdvancedBot.Core.Services.Commands
{
    public class ChannelCounterService
    {
        private List<ChannelCounterInfo> _activeCounters = new List<ChannelCounterInfo>();
        private DiscordSocketClient _client;
        private GLRClient _glr;
        private GuildAccountService _guild;
        private Timer _timer = new Timer(6 * 60 * 1000);

        public ChannelCounterService(DiscordSocketClient client, GLRClient glr, GuildAccountService guild)
        {
            _client = client;
            _glr = glr;
            _guild = guild;

            _timer.Start();
            _timer.Elapsed += OnTimerElapsed;

            InitializeCounters();
        }

        private void OnTimerElapsed(object timerObj, ElapsedEventArgs e)
        {
            _timer.Stop();

            HandleActiveChannelCounters();

            _timer.Start();
        }

        public void InitializeCounters()
        {
            var enumValues = Enum.GetValues(typeof(ChannelCounterType)) as ChannelCounterType[];

            for (int i = 0; i < enumValues.Length; i++)
            {
                _activeCounters.Add(new ChannelCounterInfo(enumValues[i]));
            }
        }

        public ChannelCounterInfo[] GetAllChannelCounters()
            => _activeCounters.ToArray();

        public ChannelCounterType ParseChannelCounterTypeFromInput(string input)
        {
            input = input.ToLower();
            ChannelCounterType type;

            var counter = _activeCounters.Find(x => x.Trigger == input);

            if (counter is null)
                throw new Exception($"'{input}' is an invalid counter, please run !counter list.");

            return counter.Type;
        }

        public void AddNewChannelCounter(ulong guildId, ChannelCounter counter)
        {
            var guild = _guild.GetOrCreateGuildAccount(guildId);
            var fCounter = guild.ChannelCounters.Find(x => x.Type == counter.Type);
            var cCounter = guild.ChannelCounters.Find(x => x.ChannelId == counter.ChannelId);

            if (fCounter != null)
                throw new Exception($"A counter of this type already exists. (channel id: {fCounter.ChannelId})");
            else if (cCounter != null)
                throw new Exception($"This channel already has the '{cCounter.Type}' active.");
                

            guild.ChannelCounters.Add(counter);
            _guild.SaveGuildAccount(guild);
        }

        public void RemoveChannelCounterByType(ulong guildId, ChannelCounterType counterType)
        {
            var guild = _guild.GetOrCreateGuildAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.Type == counterType);

            if (counter is null)
                throw new Exception($"There is no counter active of type '{counterType}'.");
            
            guild.ChannelCounters.Remove(counter);

            _guild.SaveGuildAccount(guild);
        }

        public void RemoveChannelCounterByChannel(ulong guildId, ulong channelId)
        {
            var guild = _guild.GetOrCreateGuildAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.ChannelId == channelId);

            if (counter is null)
                throw new Exception($"This channel has no active counter.");
            
            guild.ChannelCounters.Remove(counter);

            _guild.SaveGuildAccount(guild);
        }

        private void HandleActiveChannelCounters()
        {
            var guilds = _guild.GetAllGuilds();
            var flashInfo = GetFlashServerInfo();
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
                            string newName = $"Flash Status: {flashInfo.ServerStatus}";
                            if (channel.Name != newName)
                                channel.ModifyAsync(x => x.Name = newName);
                            break;
                        case ChannelCounterType.PAStatus:
                            string newName1 = $"Mobile Status: {paStatus}";
                            if (channel.Name != newName1)
                                channel.ModifyAsync(x => x.Name = newName1);
                            break;
                        case ChannelCounterType.OnlinePlayers:
                            string newName2 = $"Flash Players: {flashInfo.OnlinePlayers}";
                            if (channel.Name != newName2)
                                channel.ModifyAsync(x => x.Name = newName2);
                            break;
                        case ChannelCounterType.TotalCommandsExecuted:
                            string newName3 = $"Game Commands: {flashInfo.TotalCommandsExecuted}";
                            if (channel.Name != newName3)
                                channel.ModifyAsync(x => x.Name = newName3);
                            break;
                        case ChannelCounterType.MemberCount:
                            string newName4 = $"Discord Members: {guild.MemberCount}";
                            if (channel.Name != newName4)
                                channel.ModifyAsync(x => x.Name = newName4);
                            break;
                        default:
                            break;
                    }
                }
            }
            
            Console.WriteLine($"Updated all active counters.");
        }

        private FlashServerInfo GetFlashServerInfo()
        {
            var newStatus = "Offline";
            var status = new ServerStatus();

            try
            {
                status = _glr.GetServerStatus().GetAwaiter().GetResult();
                if (status.Ready)
                    newStatus = "Online";
                else newStatus = "Launching";
            }
            catch (Exception exc)
            {
                newStatus = "Offline";
            }

            return new FlashServerInfo
            {
                ServerStatus = newStatus,
                OnlinePlayers = status.OnlinePlayers,
                TotalCommandsExecuted = status.TotalCommandsExecuted
            };
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
