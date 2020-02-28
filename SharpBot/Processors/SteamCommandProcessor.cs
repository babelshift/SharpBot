using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace SharpBot.Processors
{
    public class SteamCommandProcessor : IProcessor
    {
        private readonly SteamUser steamUserInterface;
        private readonly SteamUserStats steamUserStatsInterface;
        private readonly SteamId steamIdContainer;

        public SteamCommandProcessor(string steamWebApiKey)
        {
            var webInterfaceFactory = new SteamWebInterfaceFactory(steamWebApiKey);
            var httpClient = new HttpClient();
            steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(httpClient);
            steamUserStatsInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUserStats>(httpClient);
            steamIdContainer = webInterfaceFactory.CreateSteamWebInterface<SteamId>(httpClient);
        }

        public async Task<string> ProcessCommandAsync(string inputMessage)
        {
            if (inputMessage == "!steam")
            {
                return "!steam [command] [username] to use various Steam commands. Valid commands are 'id', 'user', 'bans', 'friends', 'achievements'.";
            }
            else if (inputMessage.StartsWith("!steam"))
            {
                var splitParsedMessage = inputMessage.Split(' ');
                if (splitParsedMessage.Length > 2)
                {
                    string steamCommand = splitParsedMessage[1];
                    string steamCommandParameter = splitParsedMessage[2];

                    await steamIdContainer.ResolveAsync(steamCommandParameter);
                    ulong steamId = steamIdContainer.To64Bit();

                    if (steamCommand == "id")
                    {
                        return $"Steam ID for {steamCommandParameter} is {steamId}";
                    }
                    else if (steamCommand == "user")
                    {
                        var steamUser = await steamUserInterface.GetPlayerSummaryAsync(steamId);
                        return $"Steam profile for {steamCommandParameter} is {steamUser.Data.ProfileUrl} with visibility set to {steamUser.Data.ProfileVisibility}";
                    }
                    else if (steamCommand == "bans")
                    {
                        var steamBans = await steamUserInterface.GetPlayerBansAsync(steamId);

                        // let's just look at the first ban for now
                        var ban = steamBans.Data.ToList()[0];
                        string banMessage = $"{steamCommandParameter} has {ban.NumberOfGameBans} game bans and {ban.NumberOfVACBans} VAC bans. Community banned: {ban.CommunityBanned}. VAC banned: {ban.VACBanned}. Economy banned: {ban.EconomyBan}.";
                        if (ban.NumberOfGameBans > 0 || ban.NumberOfVACBans > 0)
                        {
                            banMessage += $" Days since last ban: {ban.DaysSinceLastBan}.";
                        }
                        return banMessage;
                    }
                    else if (steamCommand == "friends")
                    {
                        var steamFriends = await steamUserInterface.GetFriendsListAsync(steamId);
                        return $"{steamCommandParameter} has {steamFriends.Data.Count} friends";
                    }
                    else if (steamCommand == "achievements")
                    {
                        var steamAchievements = await steamUserStatsInterface.GetPlayerAchievementsAsync(373420, steamId);
                        var completedAchievements = steamAchievements.Data.Achievements.Where(x => x.Achieved == 1);
                        return $"{steamCommandParameter} has {completedAchievements.Count()} achievemnts in {steamAchievements.Data.GameName}";
                    }
                }
            }

            return string.Empty;
        }
    }
}
