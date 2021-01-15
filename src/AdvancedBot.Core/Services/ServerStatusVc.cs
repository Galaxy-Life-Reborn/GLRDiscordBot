using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using GLR.Net;

namespace AdvancedBot.Core.Services
{
    public class ServerStatusVc
    {
        private DiscordSocketClient _client;
        private GLRClient _glr;
        private Timer _timer = new Timer(2 * 60 * 1000);
        private string _prefix = "Flash Status: ";
        private string _paPrefix = "PA Status: ";
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
                else newStatus = "Launching";
            }
            catch (TaskCanceledException exc)
            {
                newStatus = "Offline";
            }

            if (_lastStatus == newStatus)
                return;

            channel.ModifyAsync(x => x.Name = $"{_prefix}{newStatus}");
            _lastStatus = newStatus;


            var vc = _client.GetGuild(638856299643273257).GetVoiceChannel(791664466176770088);
            newStatus = "Offline";

            try
            {
                var test = new HttpClient().GetAsync("http://pa.galaxylifereborn.com/star/status").GetAwaiter().GetResult();
                newStatus = "Online";
            }
            catch (Exception exc)
            {
                newStatus = "Offline";
            }

            vc.ModifyAsync(x => x.Name = $"{_paPrefix}{newStatus}");
        }

        public void Initialize()
        {
            _timer.Start();
        }
    }
}
