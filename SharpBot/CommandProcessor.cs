using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharpBot
{
    public class CommandProcessor
    {
        private readonly SteamUser steamUserInterface;
        private readonly SteamUserStats steamUserStatsInterface;
        private readonly SteamId steamIdContainer;
        private readonly IrcClient client;

        public CommandProcessor(string steamWebApiKey, IrcClient client)
        {
            var webInterfaceFactory = new SteamWebInterfaceFactory(steamWebApiKey);
            var httpClient = new HttpClient();
            steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(httpClient);
            steamUserStatsInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUserStats>(httpClient);
            steamIdContainer = webInterfaceFactory.CreateSteamWebInterface<SteamId>(httpClient);
            this.client = client;
        }

        public async Task ProcessAsync(string message)
        {
            message = message.Trim().ToLower();

            if (message == "!help")
            {
                client.SendChatMessage("!steam to integrate with Steam");
            }
            else if (message == "!steam")
            {
                client.SendChatMessage("!steam [command] [username] to use various Steam commands. Valid commands are 'id', 'user', 'bans', 'friends', 'achievements'.");
            }
            // need to clean this up into a class that handles all this parsing?
            else if (message.StartsWith("!steam"))
            {
                var splitParsedMessage = message.Split(' ');
                if (splitParsedMessage.Length > 2)
                {
                    string steamCommand = splitParsedMessage[1];
                    string steamCommandParameter = splitParsedMessage[2];

                    await steamIdContainer.ResolveAsync(steamCommandParameter);
                    ulong steamId = steamIdContainer.To64Bit();

                    // ugly, refactor the copy pasted code
                    if (steamCommand == "id")
                    {
                        client.SendChatMessage($"Steam ID for {steamCommandParameter} is {steamId}");
                    }
                    else if (steamCommand == "user")
                    {
                        var steamUser = await steamUserInterface.GetPlayerSummaryAsync(steamId);
                        client.SendChatMessage($"Steam profile for {steamCommandParameter} is {steamUser.Data.ProfileUrl} with visibility set to {steamUser.Data.ProfileVisibility}");
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
                        client.SendChatMessage(banMessage);
                    }
                    else if (steamCommand == "friends")
                    {
                        var steamFriends = await steamUserInterface.GetFriendsListAsync(steamId);
                        client.SendChatMessage($"{steamCommandParameter} has {steamFriends.Data.Count} friends");
                    }
                    else if (steamCommand == "achievements")
                    {
                        var steamAchievements = await steamUserStatsInterface.GetPlayerAchievementsAsync(373420, steamId);
                        var completedAchievements = steamAchievements.Data.Achievements.Where(x => x.Achieved == 1);
                        client.SendChatMessage($"{steamCommandParameter} has {completedAchievements.Count()} achievemnts in {steamAchievements.Data.GameName}");
                    }
                }
            }
        }
    }
}