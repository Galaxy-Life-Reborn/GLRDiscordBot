using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using GLR.Net;
using GLR.Net.Entities;

namespace AdvancedBot.Core.Services
{
    public class ServerStatusVc
    {
        private DiscordSocketClient _client;
        private GLRClient _glr;
        private Timer _timer = new Timer(2 * 60 * 1000);
        private string _prefix = "Flash Status: ";
        private string _lastStatus = "Offline";

        public ServerStatusVc(DiscordSocketClient client, GLRClient glr)
        {
            _client = client;
            _glr = glr;

            _timer.Elapsed += OnTimerElapsed;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var channel = _client.GetGuild(638856299643273257).GetVoiceChannel(783698960069689365);
            var newStatus = "";

            try
            {
                var status = _glr.GetServerStatus().GetAwaiter().GetResult();
                if (status.Ready)
                    newStatus = "Online";
                else newStatus = "Offline";
            }
            catch (TaskCanceledException exc)
            {
                newStatus = "Offline";
            }

            if (_lastStatus == newStatus)
                return;

            channel.ModifyAsync(x => x.Name = $"{_prefix}{newStatus}");
            _lastStatus = newStatus;
        }

        public void Initialize()
        {
            _timer.Start();
        }
    }
}
