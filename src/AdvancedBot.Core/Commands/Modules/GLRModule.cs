﻿using AdvancedBot.Core.Commands;
using Discord;
using Discord.Commands;
using GLR.Net;
using GLR.Net.Entities;
using GLR.Net.Entities.Enums;
using Humanizer;
using Humanizer.Localisation;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserStatus = GLR.Net.Entities.Enums.UserStatus;

namespace GLR.Core.Commands.Modules
{
    [Name("glr")]
    public class ProfileModule : TopModule
    {
        private GLRClient _client;
        private string _basePath = Directory.GetCurrentDirectory();

        public ProfileModule(GLRClient client)
        {
            _client = client;
        }

        [Command("profile", RunMode = RunMode.Async)]
        [Summary("Displays a user's GLR profile.")]
        public async Task ShowProfile([Remainder]string user = "")
        {
            if (string.IsNullOrEmpty(user)) user = Context.User.Username;

            var profile = await _client.GetUserInfo(user);

            var displayRank = profile.RankInfo == Rank.Banned ? "**Banned**" : profile.RankInfo == Rank.Locked ? "**Locked**" : $"{profile.RankInfo.Humanize(LetterCasing.Title)}";

            var embed = new EmbedBuilder()
                .WithTitle($"Game Profile of {profile.Username}")
                .WithUrl(profile.ProfileUrl)
                .WithThumbnailUrl(profile.ImageUrl)
                .WithDescription($"\nThis user has the Universe ID **{profile.Id}**." +
                                $"\n**{profile.Username}** has the rank {displayRank}.")
                .AddField("Friends", $"This user currently has **{profile.Friends.Length}** friends.")
                .WithFooter($"Account created on {profile.CreatedAt.Value.ToLongDateString()}")
                .WithColor(GetColourBasedOnRank(profile.RankInfo))
                .Build();

            await ReplyAsync("", false, embed);
        }

        [Command("friends", RunMode = RunMode.Async)]
        [Summary("Displays the GLR friends of a certain user.")]
        public async Task Friends([Remainder]string user = "")
        {
            if (string.IsNullOrEmpty(user)) user = Context.User.Username;

            var profile = await _client.GetUserInfo(user);

            if (profile.Friends is null) await ReplyAsync("User doesn't have any friends!");
            
            var displayTexts = profile.Friends.Select(x => $"**{x.Username}**");

            var templateEmbed = new EmbedBuilder()
                                .WithTitle($"Friends for {profile.Username}")
                                .WithColor(Color.DarkBlue)
                                .WithAuthor("", "", "")
                                .WithFooter("Friends are ordered by the day you added them.", "");
            await SendPaginatedMessageAsync(null, displayTexts, templateEmbed);
        }

        [Command("statistics")][Alias("stats")]
        [Summary("Displays the GLR statistics for a certain user.")]
        public async Task Stats([Remainder]string user = "")
        {
            if (string.IsNullOrEmpty(user)) user = Context.User.Username;

            var profile = await _client.GetUserAsync(user);

            var displayAlliance = profile.Statistics.Alliance == "None" ? "User is not in any alliance." : $"User is part of **{profile.Statistics.Alliance}**.";

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"Statistics for {profile.Info.Username} ({profile.Info.Id})",
                Url = profile.Info.ProfileUrl,
                Color = Color.DarkMagenta,
                ThumbnailUrl = $"https://web.galaxylifereborn.com/accounts/avatars/{profile.Info.Id}.png?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Description = $"{displayAlliance}\nUser is level **{profile.Statistics.Level}**.\n\u200b"
            }
            .AddField("Experience", FormatNumbers(profile.Statistics.Experience), true)
            .AddField("Starbase", profile.Statistics.Starbase, true)
            .AddField("Colonies", profile.Statistics.Colonies, true)
            .AddField("Missions", profile.Statistics.MissionsCompleted, true)
            .AddField("Status", profile.Statistics.Status, true)
            .AddField("Attacks Done", profile.Statistics.AttacksDone, true)
            .WithFooter($"Requested by {Context.User.Username} | {Context.User.Id}")
            .Build());
        }

        [Command("advancedstats")]
        [Alias("astats", "as")]
        [Summary("Shows advanced statistics for said user.")]
        public async Task ShowAdvancedStatsAsync([Remainder]string user = "")
        {
            if (string.IsNullOrEmpty(user)) user = Context.User.Username;

            var profile = await _client.GetUserAsync(user);
            var stats = await _client.GetAdvancedStatisticsAsync(profile.Info.Id);

            var embed = new EmbedBuilder()
            {
                Title = $"Advanced Statistics for {profile.Info.Username}",
                Url = profile.Info.ProfileUrl,
                Color = Color.DarkMagenta,
                ThumbnailUrl = $"https://web.galaxylifereborn.com/accounts/avatars/{profile.Info.Id}.png?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            }
            .AddField("Level", profile.Statistics.Level, true)
            .AddField("Players Attacked", stats.PlayersAttacked, true)
            .AddField("Npcs Attacked", stats.NpcsAttacked, true)
            .AddField("Chips Spent", FormatNumbers(stats.ChipsSpent), true)
            .AddField("Coins Spent", FormatNumbers(stats.CoinsSpent), true)
            .AddField("Minerals Spent", FormatNumbers(stats.MineralsSpent), true)
            .AddField("Friends Helped", FormatNumbers(stats.FriendsHelped), true)
            .AddField("Gifts Received", FormatNumbers(stats.GiftsReceived), true)
            .AddField("Gifts Sent", FormatNumbers(stats.GiftsSent), true)
            .AddField("PlayTime", TimeSpan.FromMilliseconds(stats.TotalPlayTimeInMs).Humanize(3, minUnit: TimeUnit.Minute), true)
            .AddField("Nukes Used", stats.NukesUsed, true)
            .AddField("Obstacles Recycled", stats.ObstaclesRecycled, true);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("alliance")]
        [Summary("Shows basic information about an alliance.")]
        public async Task GetAllianceAsync([Remainder]string input)
        {
            var alliance = await _client.GetAllianceByName(input);

            if (alliance is null)
                throw new Exception($"Could not find alliance corresponding with {input}");

            var emblemFileName = $"flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png";

            var embed = new EmbedBuilder()
            .WithTitle(alliance.Name)
            .WithDescription($"<:AFECounselor_Mobius:639094741631369247> Alliance owned by **{alliance.Owner.Name}** ({alliance.Owner.Id})\n\u200b")
            .WithColor(Color.DarkPurple)
            .WithThumbnailUrl($"attachment://{emblemFileName}")
            .AddField("Members", alliance.MemberCount, true)
            .AddField("Wars Participated", 0, true)
            .AddField("Wars won", 0, true)
            .WithFooter($"Run !members {input} to see its members.")
            .Build();

            //await Context.Channel.SendFileAsync($"{_basePath}/Emblems/{emblemFileName}", embed: embed);
            await Context.Channel.SendMessageAsync(embed: embed);
        }

        [Command("members")]
        [Summary("Shows all members of an alliance.")]
        public async Task GetAllianceMembersAsync([Remainder]string input)
        {
            var allianceInfo = await _client.GetAllianceByName(input);
            var allianceMembers = await _client.GetAllianceMembers(input);
            var displayTexts = allianceMembers.Select(x => $"**{x.Name}**");

            var templateEmbed = new EmbedBuilder()
                .WithTitle($"Members of {allianceInfo.Name}")
                .WithColor(Color.Purple)
                .WithAuthor("", "", "");

            await SendPaginatedMessageAsync(null, displayTexts, templateEmbed);
        }

        // [Command("stat", RunMode = RunMode.Async)]
        // [Summary("Test stats command in the works.")]
        // public async Task DisplayStatWithImage([Remainder]string user = "")
        // {
        //     var templateHtml = File.ReadAllText("stats/TemplateCard.html");

        //     if (string.IsNullOrEmpty(user)) user = Context.User.Username;

        //     var profile = await _client.GetUserAsync(user);

        //     var newHtml = FormatHtmlForStats(templateHtml, profile.Info, profile.Statistics);
        //     File.WriteAllText($"stats/{user}.html", newHtml);

        //     await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
        //     var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        //     {
        //         Args = new string[] { "--no-sandbox"},
        //         Headless = true,
        //         DefaultViewport = new ViewPortOptions()
        //         {
        //             Width = 426,
        //             Height = 206
        //         }
        //     });
        //     var page = await browser.NewPageAsync();
        //     await page.GoToAsync($"file:///{Environment.CurrentDirectory}/stats/{user}.html");

        //     await page.ScreenshotAsync($"{Environment.CurrentDirectory}/stats/{user}.png");
        //     browser.Dispose();

        //     await Context.Channel.SendFileAsync($"stats/{user}.png");
        // }

        [Command("status")]
        [Summary("Shows the current status of the flash servers.")]
        public async Task DisplayServerStatusAsync()
        {
            ServerStatus status = null;

            try
            {
                status = await _client.GetServerStatus();
            }
            catch (TaskCanceledException e)
            {
                await ReplyAsync("Request timed out, servers are off.");
                return;
            }
            
            if (!status.Ready)
            {
                await ReplyAsync($"Servers have been launching for **{(DateTime.UtcNow - status.OnlineSince).Humanize()}**");
                return;
            }

            var embed = new EmbedBuilder()
            {
                Title = $"GLR Server Status",
                Description = $"Servers have been **online** since **{status.OnlineSince.ToString()}** (miracle innit)",
                Color = Color.Blue
            }
            .AddField("Total Commands Executed", FormatNumbers(status.TotalCommandsExecuted), true)
            .AddField("Commands Executed", FormatNumbers(status.CommandsExecutedSinceLaunch), true)
            .AddField("Total Stars", status.TotalStars, false)
            .AddField("Total Players", status.TotalPlayers, true)
            .AddField("Online Players", status.OnlinePlayers, true);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("leaderboard")][Alias("lb")]
        [Summary("Displays the top 100 users level or chips-wise.")]
        public async Task DisplayLeaderboards(string leaderboardType = "levels")
        {
            IList<string> displayTexts = new string[] { "Error while retrieving data." };

            if (leaderboardType == "chips") displayTexts = (await _client.GetTopChipsPlayers()).Select(x => $"**{x.Name}** ({x.Chips} <:CABGalaxy_Chip:714111537366958130>)").ToArray();
            else if (leaderboardType == "levels") displayTexts = (await _client.GetTopLevelPlayers()).Select(x => $"**{x.Name}** ({x.Level} <:CADExperience:714553640848719912>)").ToArray();
            else throw new Exception("Wrong leaderboard type. Either choose `levels` or `chips`.");
            
            for (int i = 0; i < displayTexts.Count(); i++)
            {
                displayTexts[i] = $"**#{i + 1}** | {displayTexts[i]}\n";
            }

            var templateEmbed = new EmbedBuilder()
                                .WithTitle($"{leaderboardType.ToUpperInvariant()} Leaderboard")
                                .WithColor(Color.Purple)
                                .WithAuthor("", "", "");

            await SendPaginatedMessageAsync(null, displayTexts, templateEmbed);
        }
    
        [Command("compare")][Alias("c")]
        [Summary("Compares 2 GLR users.")]
        public async Task CompareStats(string baseUser, string secondUser)
        {
            if (baseUser.ToLower() == secondUser.ToLower()) throw new Exception("Please enter two different users to compare.");

            var baseUserStats = await _client.GetUserAsync(baseUser);

            var secondUserStats = await _client.GetUserAsync(secondUser);

            var expDifference = Math.Round((decimal)baseUserStats.Statistics.Experience / secondUserStats.Statistics.Experience, 2);

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"Comparison between {baseUserStats.Info.Username} & {secondUserStats.Info.Username}",
                Description = $"{baseUserStats.Info.Username} has **{expDifference}x** the experience of {secondUserStats.Info.Username}\n" +
                              $"Difference of **{FormatNumbers(Math.Abs((decimal)baseUserStats.Statistics.Experience - secondUserStats.Statistics.Experience))}** experience.\n\n" + 
                              $"{baseUserStats.Info.Username} has **{FormatNumbers(baseUserStats.Statistics.Experience)}** experience and is level **{baseUserStats.Statistics.Level}**.\n" +
                              $"{secondUserStats.Info.Username} has **{FormatNumbers(secondUserStats.Statistics.Experience)}** experience and is level **{secondUserStats.Statistics.Level}**.",
                Color = expDifference > 1 ? Color.DarkGreen : Color.DarkOrange
            }
            .Build());
        }

        [Command("compare")][Alias("c")]
        [Summary("Compares 2 GLR users.")]
        public async Task CompareStats(string userToCompare)
            => await CompareStats(Context.User.Username, userToCompare);

        [Command("buildawall")]
        [Summary("Advanced compare of 2 GLR users.")]
        public async Task GetNecessaryExpForLevel(string baseUser, string userToCompare, [Remainder]string fact = "")
        {
            if (baseUser.ToLower() == userToCompare.ToLower()) throw new Exception("Please enter two different users to compare.");

            var baseUserStats = await _client.GetUserAsync(baseUser);

            var secondUserStats = await _client.GetUserAsync(userToCompare);

            var expToGain = (long) secondUserStats.Statistics.Experience - (long) baseUserStats.Statistics.Experience;
            
            if (expToGain < 0) throw new Exception($"{baseUserStats.Info.Username} is already ahead of {secondUserStats.Info.Username}");
            else if (expToGain == 0) throw new Exception($"{baseUserStats.Info.Username} & {secondUserStats.Info.Username} have equally as much exp.");
            
            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"Exp needed to beat {secondUserStats.Info.Username}",
                Description = $"{baseUserStats.Info.Username} has **{FormatNumbers(baseUserStats.Statistics.Experience)}** experience and is level **{baseUserStats.Statistics.Level}**.\n" +
                              $"{secondUserStats.Info.Username} has **{FormatNumbers(secondUserStats.Statistics.Experience)}** experience and is level **{secondUserStats.Statistics.Level}**.\n\n" +
                              $"{GetVisualizationForProgressNecessary(expToGain, fact)}",
                Color = Color.DarkMagenta
            }
            .Build());
        }

        private uint GetColourBasedOnRank(Rank rank)
        {
            switch (rank)
            {
                case Rank.Developer:
                    return 480472;
                case Rank.Administrator:
                    return 3172029;
                case Rank.GameModerator:
                    return 34122;
                case Rank.Moderator:
                    return 2605694;
                case Rank.TicketModerator:
                    return 65423;
                case Rank.ContentCreator:
                    return 16729674;
                case Rank.Grandmaster:
                    return 15790320;
                case Rank.Tester:
                    return 7284617;
                case Rank.UltraSupporter:
                    return 7419530;
                case Rank.Supporter:
                    return 15710778;
                case Rank.Locked:
                    return 000000;
                case Rank.Banned:
                    return 000000;
                default:
                    return 16777215;
            }
        }

        private string GetVisualizationForProgressNecessary(decimal expDifference, string fact = "")
        {
            var r = new Random();

            uint wallToLevel5Price = 248000;
            uint expPerWallToLevel5 = 641;

            uint sbToLevel9Price = 121000000;
            uint expPerSbToLevel9 = 313701;

            uint expPerFirebitAttack = 38990;

            uint expPerWarpGateLevel6Destroyed = 3541;

            if (fact == "") 
            {
                var random = r.Next(0, 3);

                if (random == 0) fact = "firebit";
                else if (random == 1) fact = "walls";
                else if (random == 2) fact = "sb";
                else if (random == 3) fact = "warp gates";
            }

            switch (fact)
            {
                case "firebit": return $"User needs to fully defeat firebit **{FormatNumbers(Math.Round(expDifference / expPerFirebitAttack, 0))}** more times to beat them.";
                case "walls": return $"User needs to upgrade **{FormatNumbers(Math.Round(expDifference / expPerWallToLevel5, 0))}** more walls from lvl 4 to 5 to beat them.\n" +
                                     $"This would cost **{FormatNumbers(Math.Round((expDifference / expPerWallToLevel5) * wallToLevel5Price, 0))}** gold";
                case "sb": return $"User needs to upgrade **{FormatNumbers(Math.Round(expDifference / expPerSbToLevel9, 0))}** more starbases from level 8 to 9 to beat them.\n" + 
                                  $"This would cost **{FormatNumbers(Math.Round((expDifference / expPerSbToLevel9) * sbToLevel9Price, 0))}** gold.";
                case "warp gates": return $"User would need to destroy **{FormatNumbers(Math.Round(expDifference / expPerWarpGateLevel6Destroyed))}** more warp gates lvl 6 to beat them.";
                
                default: return $"Please enter a valid value (`firebit`, `walls`, `sb` or `warp gates`).";
            }
        }

        private string FormatNumbers(decimal experiencePoints)
        {
            // 1bil<
            if (experiencePoints > 1000000000) return $"{Math.Round(experiencePoints / 1000000000, 2)}B";

            // 10mil< 
            else if (experiencePoints > 10000000) return $"{Math.Round(experiencePoints / 1000000, 1)}M";

            // 1mil< 
            else if (experiencePoints > 1000000) return $"{Math.Round(experiencePoints / 1000000, 2)}M";

            // 100K<
            else if (experiencePoints > 10000) return $"{Math.Round(experiencePoints / 1000, 1)}K";

            // 10K<
            else if (experiencePoints > 10000) return $"{Math.Round(experiencePoints / 1000, 2)}K";

            else return experiencePoints.ToString();
        }
    
        private string FormatHtmlForStats(string original, UserInfo profile, Statistics stats)
        {
            var pieces = original.Split('|');
            //pieces[1] = profile.ImageUrl;
            pieces[3] = profile.Username;
            pieces[5] = stats.Alliance;
            pieces[7] = stats.Level.ToString();
            pieces[9] = profile.Friends.Length.ToString();
            pieces[11] = stats.Starbase.ToString();
            pieces[13] = profile.CreatedAt.Value.ToShortDateString();
            pieces[15] = stats.Status == UserStatus.Online ? "https://i.imgur.com/cuFVhuL.png" : "https://i.imgur.com/qHRoNBA.png";
            pieces[17] = stats.Status == UserStatus.Online ? "Online" : "Offline";
            pieces[19] = stats.Colonies.ToString();
            pieces[21] = stats.AttacksDone.ToString();

            return string.Join("", pieces);
        }
    }
}
