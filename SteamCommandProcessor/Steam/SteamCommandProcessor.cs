using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BotCommandFunctions
{
    public class SteamCommandProcessor
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly SteamWebInterfaceFactory webInterfaceFactory;
        private readonly SteamUser steamUserInterface;
        private readonly SteamUserStats steamUserStatsInterface;
        private readonly SteamId steamIdContainer;
        private readonly PlayerService playerServiceInterface;
        private readonly SteamNews steamNewsInterface;

        public SteamCommandProcessor(string steamWebApiKey)
        {
            webInterfaceFactory = new SteamWebInterfaceFactory(steamWebApiKey);
            steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(httpClient);
            steamUserStatsInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUserStats>(httpClient);
            steamIdContainer = webInterfaceFactory.CreateSteamWebInterface<SteamId>(httpClient);
            playerServiceInterface = webInterfaceFactory.CreateSteamWebInterface<PlayerService>(httpClient);
            steamNewsInterface = webInterfaceFactory.CreateSteamWebInterface<SteamNews>(httpClient);
        }

        /// <summary>
        /// Queries the Steam Web API for the Steam App List. Useful when we call other Steam Web API endpoints that require an App ID.
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<uint, string>> GetSteamAppListAsync()
        {
            var steamAppsInterface = webInterfaceFactory.CreateSteamWebInterface<SteamApps>(httpClient);
            var appListResponse = await steamAppsInterface.GetAppListAsync();
            return appListResponse.Data.ToDictionary(x => x.AppId, x => x.Name);
        }

        public async Task<string> ProcessCommandAsync(string inputMessage, IDictionary<uint, string> appList)
        {
            if (inputMessage == "!steam")
            {
                return "!steam [command] [username] to use various Steam commands. Valid commands are 'id', 'user', 'bans', 'friends', 'achievements'.";
            }
            else if (inputMessage.StartsWith("!steam"))
            {
                var splitParsedMessage = inputMessage.Split(' ');
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
                    string response = $"Steam profile for {steamCommandParameter} is {steamUser.Data.ProfileUrl} . Account created on: {steamUser.Data.AccountCreatedDate.ToShortDateString()}. User status is: {steamUser.Data.UserStatus}.";
                    if (!string.IsNullOrWhiteSpace(steamUser.Data.PlayingGameName))
                    {
                        response += $" Currently playing: {steamUser.Data.PlayingGameName}.";
                    }
                    return response;
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
                    var appId = uint.Parse(splitParsedMessage[3]);
                    var steamAchievements = await steamUserStatsInterface.GetPlayerAchievementsAsync(appId, steamId);
                    var completedAchievements = steamAchievements.Data.Achievements.Where(x => x.Achieved == 1);
                    return $"{steamCommandParameter} has {completedAchievements.Count()} achievements in {steamAchievements.Data.GameName}";
                }
                else if (steamCommand == "players")
                {
                    var appId = uint.Parse(splitParsedMessage[2]);
                    var steamPlayers = await steamUserStatsInterface.GetNumberOfCurrentPlayersForGameAsync(appId);
                    var gameName = appList[appId];
                    return $"{gameName} has {steamPlayers.Data} current players";
                }
                else if (steamCommand == "level")
                {
                    var steamLevel = await playerServiceInterface.GetSteamLevelAsync(steamId);
                    return $"{steamCommandParameter} is level {steamLevel.Data} on Steam";
                }
                else if (steamCommand == "badges")
                {
                    var steamBadges = await playerServiceInterface.GetBadgesAsync(steamId);
                    return $"{steamCommandParameter} is level {steamBadges.Data.PlayerLevel} which required {steamBadges.Data.PlayerXpNeededCurrentLevel} XP. {steamCommandParameter} has {steamBadges.Data.PlayerXp} XP and needs {steamBadges.Data.PlayerXpNeededToLevelUp} XP to level up.";
                }
                else if (steamCommand == "recent")
                {
                    var recentGames = await playerServiceInterface.GetRecentlyPlayedGamesAsync(steamId);
                    var gameList = recentGames.Data.RecentlyPlayedGames.Select(x => $"{x.Name} (Total: {Math.Round((double)x.PlaytimeForever / 60, 1)} hrs, 2 Wks: {Math.Round((double)x.Playtime2Weeks / 60, 1)} hrs)");
                    string joinedGameList = string.Join(", ", gameList);
                    string response = $"{steamCommandParameter} has played {recentGames.Data.TotalCount} games in the last 2 weeks: {joinedGameList}.";
                    return response;
                }
                else if (steamCommand == "news")
                {
                    var appId = uint.Parse(splitParsedMessage[2]);
                    var gameName = appList[appId];
                    var news = await steamNewsInterface.GetNewsForAppAsync(appId, 1);
                    var recentNews = news.Data.NewsItems.ToList()[0];
                    return $"Here's the latest news for {gameName}. Author: {recentNews.Author}, Title: {recentNews.Title}, Url: {recentNews.Url} .";
                }
            }

            return string.Empty;
        }
    }
}