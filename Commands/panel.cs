using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		private static async Task<string> GetProfilePictureAsync(string steamId64, bool small = false)
		{
			string size = small ? "avatarMedium" : "avatarFull";
			try
			{
				string apiUrl = $"https://steamcommunity.com/profiles/{steamId64}/?xml=1";

				HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

				if (response.IsSuccessStatusCode)
				{
					string xmlResponse = await response.Content.ReadAsStringAsync();
					int startIndex = xmlResponse.IndexOf($"<{size}><![CDATA[") + $"<{size}><![CDATA[".Length;
					int endIndex = xmlResponse.IndexOf($"]]></{size}>", startIndex);
					string profilePictureUrl = xmlResponse.Substring(startIndex, endIndex - startIndex);

					return profilePictureUrl;
				}
				else
				{
					Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
					return null!;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception: {ex.Message}");
				return null!;
			}
		}

		static int CountLetters(string input)
		{
			int count = 0;

			foreach (char c in input)
			{
				if (char.IsLetter(c))
				{
					count++;
				}
			}

			return count;
		}

		[ConsoleCommand("css_panel_say", "Say to all players from the panel.")]
		[CommandHelper(1, "<message>")]
		[RequiresPermissions("@css/root")]
		public void OnPanelSayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

			byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
			string utf8String = Encoding.UTF8.GetString(utf8BytesString);

			foreach (CCSPlayerController player in Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.Connected == PlayerConnectedState.PlayerConnected && !p.IsBot && !p.IsHLTV))
			{
				player.PrintToChat(StringExtensions.ReplaceColorTags(utf8String));
			}
		}

		/**
		* Prints the server info and a list of players to the console
		*/
		[ConsoleCommand("css_query")]
		[CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
		[RequiresPermissions("@css/root")]
		public void OnQueryCommand(CCSPlayerController? caller, CommandInfo command)
		{
			List<CCSPlayerController> playersToTarget = Utilities.GetPlayers().Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV).ToList();

			string Map = Server.MapName;
			int Players = playersToTarget.Count;
			int MaxPlayers = Server.MaxPlayers;
			string[] Maps;

			try
			{
				Maps = Server.GetMapList();
			}
			catch (Exception)
			{
				Maps = Array.Empty<string>(); // return an empty array
			}

			var server = new
			{
				map = Map,
				players = Players,
				maxPlayers = MaxPlayers,
				maps = Maps,
				pluginVersion = ModuleVersion
			};

			List<Task<object>> playerTasks = playersToTarget
			.FindAll(player => !player.IsBot && !player.IsHLTV && player.PlayerName != "")
			.Select(player =>
			{
				string deaths = player.ActionTrackingServices!.MatchStats.Deaths.ToString();
				string headshots = player.ActionTrackingServices!.MatchStats.HeadShotKills.ToString();
				string assists = player.ActionTrackingServices!.MatchStats.Assists.ToString();
				string damage = player.ActionTrackingServices!.MatchStats.Damage.ToString();
				string kills = player.ActionTrackingServices!.MatchStats.Kills.ToString();
				string time = player.ActionTrackingServices!.MatchStats.LiveTime.ToString();
				var user = new
				{
					userId = player.UserId,
					playerName = player.PlayerName,
					ipAddress = player.IpAddress?.Split(":")[0],
					// accountId = player.AuthorizedSteamID?.AccountId.ToString(),
					// steamId2 = player.AuthorizedSteamID?.SteamId2,
					// steamId3 = player.AuthorizedSteamID?.SteamId3,
					steam64 = player.AuthorizedSteamID?.SteamId64.ToString(),
					ping = player.Ping,
					team = player.Team,
					// clanName = player.ClanName,
					kills,
					deaths,
					// assists,
					// headshots,
					// damage,
					score = player.Score,
					// roundScore = player.RoundScore,
					// roundsWon = player.RoundsWon,
					mvps = player.MVPs,
					// time, // ? Fix this, it's not the time the player has been connected
					// avatar = player.AuthorizedSteamID != null ? await GetProfilePictureAsync(player.AuthorizedSteamID.SteamId64.ToString(), true) : ""
				};
				return Task.FromResult((object)user);
			}).ToList();

			List<object> players = new List<object>();
			try
			{
				players = Task.WhenAll(playerTasks).Result.ToList();
			}
			catch (AggregateException ex)
			{
				foreach (var innerEx in ex.InnerExceptions)
				{
					Logger.LogError(innerEx, "Error while querying players");
				}
			}

			string jsonString = JsonConvert.SerializeObject(
				new
				{
					server,
					players
				}
			);

			Server.PrintToConsole(jsonString);
		}
	}
}
