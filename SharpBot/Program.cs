using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace SharpBot
{
    internal class Program
    {
        private static IrcClientProperties props;
        private static SteamUser steamUserInterface;
        private static SteamUserStats steamUserStatsInterface;

        private static async Task Main(string[] args)
        {
            var config = await File.ReadAllTextAsync("appsettings.json");
            props = JsonConvert.DeserializeObject<IrcClientProperties>(config);

            using (var client = new IrcClient(props.TwitchIrcUrl, props.TwitchIrcPort, props.BotUsername, props.TwitchOAuthToken, props.ChannelName))
            {
                // this will send pings, but do we also need to reply to pings from twitch per documentation?
                var pinger = new Pinger(client);
                pinger.Start();

                var webInterfaceFactory = new SteamWebInterfaceFactory(props.SteamWebApiKey);
                var httpClient = new HttpClient();
                steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(httpClient);
                steamUserStatsInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUserStats>(httpClient);

                // listen for commands forever
                while (true)
                {
                    Console.WriteLine("Reading message");
                    var message = client.ReadMessage();
                    Console.WriteLine($"Read message: {message}");

                    var parsedMessage = ParseMessage(message);
                    await HandleParsedMessageAsync(client, parsedMessage);
                }
            }
        }

        private static async Task HandleParsedMessageAsync(IrcClient client, string parsedMessage)
        {
            if (parsedMessage == "!help")
            {
                client.SendChatMessage("!steam to integrate with Steam");
            }
            else if(parsedMessage == "!steam")
            {
                client.SendChatMessage("!steam id [username] to see a user's Steam ID");
                client.SendChatMessage("!steam user [username] to see a user's profile information");
            }
            // need to clean this up into a class that handles all this parsing?
            else if(parsedMessage.StartsWith("!steam"))
            {
                var splitParsedMessage = parsedMessage.Split(' ');
                if (splitParsedMessage.Length > 2)
                {
                    var subCommand = splitParsedMessage[1];
                    var steamWebRequest = CreateSteamWebRequest(props.SteamWebApiBaseUrl, props.SteamWebApiKey);

                    // ugly, refactor the copy pasted code
                    if (subCommand == "id")
                    {
                        var subCommandParameter = splitParsedMessage[2];
                        var steamIdHelper = new SteamId(subCommandParameter, steamWebRequest);
                        ulong steamId = steamIdHelper.To64Bit();
                        client.SendChatMessage($"Steam ID for {subCommandParameter} is {steamId}");
                    }
                    else if(subCommand == "user")
                    {
                        var subCommandParameter = splitParsedMessage[2];
                        var steamIdHelper = new SteamId(subCommandParameter, steamWebRequest);
                        ulong steamId = steamIdHelper.To64Bit();
                        var steamUser = await steamUserInterface.GetPlayerSummaryAsync(steamId);
                        client.SendChatMessage($"Steam profile for {subCommandParameter} is {steamUser.Data.ProfileUrl} with visibility set to {steamUser.Data.ProfileVisibility}");
                    }
                    else if(subCommand == "bans")
                    {
                        var subCommandParameter = splitParsedMessage[2];
                        var steamIdHelper = new SteamId(subCommandParameter, steamWebRequest);
                        ulong steamId = steamIdHelper.To64Bit();
                        var steamBans = await steamUserInterface.GetPlayerBansAsync(steamId);

                        // let's just look at the first ban for now
                        var ban = steamBans.Data.ToList()[0];
                        string banMessage = $"{subCommandParameter} has {ban.NumberOfGameBans} game bans and {ban.NumberOfVACBans} VAC bans. Community banned: {ban.CommunityBanned}. VAC banned: {ban.VACBanned}. Economy banned: {ban.EconomyBan}.";
                        if(ban.NumberOfGameBans > 0 || ban.NumberOfVACBans > 0)
                        {
                            banMessage += $" Days since last ban: {ban.DaysSinceLastBan}.";
                        }
                        client.SendChatMessage(banMessage);
                    }
                    else if(subCommand == "friends")
                    {
                        var subCommandParameter = splitParsedMessage[2];
                        var steamIdHelper = new SteamId(subCommandParameter, steamWebRequest);
                        ulong steamId = steamIdHelper.To64Bit();
                        var steamFriends = await steamUserInterface.GetFriendsListAsync(steamId);
                        client.SendChatMessage($"{subCommandParameter} has {steamFriends.Data.Count} friends");
                    }
                    else if(subCommand == "achievements")
                    {
                        var subCommandParameter = splitParsedMessage[2];
                        var steamIdHelper = new SteamId(subCommandParameter, steamWebRequest);
                        ulong steamId = steamIdHelper.To64Bit();

                        var steamAchievements = await steamUserStatsInterface.GetPlayerAchievementsAsync(373420, steamId);
                        var completedAchievements = steamAchievements.Data.Achievements.Where(x => x.Achieved == 1);
                        client.SendChatMessage($"{subCommandParameter} has {completedAchievements.Count()} achievemnts in {steamAchievements.Data.GameName}");
                    }
                }
            }
        }

        private static string ParseMessage(string message)
        {
            string[] splitMessage = message.Split(':');
            if (splitMessage.Length == 3)
            {
                return splitMessage[2];
            }

            return string.Empty;
        }

        private static ISteamWebRequest CreateSteamWebRequest(string steamWebApiBaseUrl, string steamWebApiKey)
        {
            HttpClient httpClient = new HttpClient();
            SteamWebHttpClient steamWebHttpClient = new SteamWebHttpClient(httpClient);
            return new SteamWebRequest(steamWebApiBaseUrl, steamWebApiKey, steamWebHttpClient);
        }
    }
}