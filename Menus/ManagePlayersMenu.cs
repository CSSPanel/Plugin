using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace CSSPanel.Menus
{
	public static class ManagePlayersMenu
	{
		public static void OpenMenu(CCSPlayerController admin)
		{
			if (admin.IsValid == false)
				return;

			var localizer = CSSPanel._localizer;
			if (AdminManager.PlayerHasPermissions(admin, "@css/generic") == false)
			{
				admin.PrintToChat(localizer?["sa_prefix"] ??
				                  "[Admin] " + 
				                  (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
				);
				return;
			}

			var menu = AdminMenu.CreateMenu(localizer?["sa_menu_players_manage"] ?? "Manage Players");
			List<ChatMenuOptionData> options = [];

			// permissions
			var hasSlay = AdminManager.PlayerHasPermissions(admin, "@css/slay");
			var hasKick = AdminManager.PlayerHasPermissions(admin, "@css/kick");
			var hasBan = AdminManager.PlayerHasPermissions(admin, "@css/ban");
			var hasChat = AdminManager.PlayerHasPermissions(admin, "@css/chat");

			// TODO: Localize options
			// options added in order

			if (hasSlay)
			{
				options.Add(new ChatMenuOptionData(localizer?["sa_slap"] ?? "Slap", () => PlayersMenu.OpenMenu(admin, localizer?["sa_slap"] ?? "Slap", SlapMenu)));
				options.Add(new ChatMenuOptionData(localizer?["sa_slay"] ?? "Slay", () => PlayersMenu.OpenMenu(admin, localizer?["sa_slay"] ?? "Slay", Slay)));
			}

			if (hasKick)
				options.Add(new ChatMenuOptionData(localizer?["sa_kick"] ?? "Kick", () => PlayersMenu.OpenMenu(admin, localizer?["sa_kick"] ?? "Kick", KickMenu)));

			if (hasBan)
				options.Add(new ChatMenuOptionData(localizer?["sa_ban"] ?? "Ban", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_ban"] ?? "Ban", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_ban"] ?? "Ban"}: {player.PlayerName}", player, BanMenu))));

			if (hasChat)
			{
				options.Add(new ChatMenuOptionData(localizer?["sa_gag"] ?? "Gag", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_gag"] ?? "Gag", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_gag"] ?? "Gag"}: {player.PlayerName}", player, GagMenu))));
				options.Add(new ChatMenuOptionData(localizer?["sa_mute"] ?? "Mute", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_mute"] ?? "Mute", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_mute"] ?? "Mute"}: {player.PlayerName}", player, MuteMenu))));
				options.Add(new ChatMenuOptionData(localizer?["sa_silence"] ?? "Silence", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_silence"] ?? "Silence", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_silence"] ?? "Silence"}: {player.PlayerName}", player, SilenceMenu))));
			}

			if (hasKick)
				options.Add(new ChatMenuOptionData(localizer?["sa_team_force"] ?? "Force Team", () => PlayersMenu.OpenMenu(admin, localizer?["sa_team_force"] ?? "Force Team", ForceTeamMenu)));

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void SlapMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_slap"] ?? "Slap"}: {player?.PlayerName}");
			List<ChatMenuOptionData> options =
			[
				// options added in order
				new ChatMenuOptionData("0 hp", () => ApplySlapAndKeepMenu(admin, player, 0)),
				new ChatMenuOptionData("1 hp", () => ApplySlapAndKeepMenu(admin, player, 1)),
				new ChatMenuOptionData("5 hp", () => ApplySlapAndKeepMenu(admin, player, 5)),
				new ChatMenuOptionData("10 hp", () => ApplySlapAndKeepMenu(admin, player, 10)),
				new ChatMenuOptionData("50 hp", () => ApplySlapAndKeepMenu(admin, player, 50)),
				new ChatMenuOptionData("100 hp", () => ApplySlapAndKeepMenu(admin, player, 100)),
			];

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void ApplySlapAndKeepMenu(CCSPlayerController admin, CCSPlayerController? player, int damage)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Instance.Slap(admin, player, damage);
			SlapMenu(admin, player);
		}

		private static void Slay(CCSPlayerController admin, CCSPlayerController? player)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Instance.Slay(admin, player);
		}

		private static void KickMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_kick"] ?? "Kick"}: {player?.PlayerName}");

			foreach (var option in CSSPanel.Instance.Config.MenuConfigs.KickReasons)
			{
				menu.AddMenuOption(option, (_, _) =>
				{
					if (player is { IsValid: true })
						Kick(admin, player, option);
				});
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Kick(CCSPlayerController admin, CCSPlayerController? player, string? reason)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Instance.Kick(admin, player, reason);
		}

		private static void BanMenu(CCSPlayerController admin, CCSPlayerController? player, int duration)
		{
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_ban"] ?? "Ban"}: {player?.PlayerName}");

			foreach (var option in CSSPanel.Instance.Config.MenuConfigs.BanReasons)
			{
				menu.AddMenuOption(option, (_, _) =>
				{
					if (player is { IsValid: true })
						Ban(admin, player, duration, option);
				});
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Ban(CCSPlayerController admin, CCSPlayerController? player, int duration, string reason)
		{
			if (player is not { IsValid: true }) return;
				
			CSSPanel.Instance.Ban(admin, player, duration, reason);
		}

		private static void GagMenu(CCSPlayerController admin, CCSPlayerController? player, int duration)
		{
			// TODO: Localize and make options in config?
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_gag"] ?? "Gag"}: {player?.PlayerName}");

			foreach (var option in CSSPanel.Instance.Config.MenuConfigs.MuteReasons)
			{
				menu.AddMenuOption(option, (_, _) =>
				{
					if (player is { IsValid: true })
						Gag(admin, player, duration, option);
				});
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Gag(CCSPlayerController admin, CCSPlayerController? player, int duration, string reason)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Gag(admin, player, duration, reason);
		}

		private static void MuteMenu(CCSPlayerController admin, CCSPlayerController? player, int duration)
		{
			// TODO: Localize and make options in config?
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_mute"] ?? "Mute"}: {player?.PlayerName}");

			foreach (var option in CSSPanel.Instance.Config.MenuConfigs.MuteReasons)
			{
				menu.AddMenuOption(option, (_, _) =>
				{
					if (player is { IsValid: true })
						Mute(admin, player, duration, option);
				});
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Mute(CCSPlayerController admin, CCSPlayerController? player, int duration, string reason)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Instance.Mute(admin, player, duration, reason);
		}

		private static void SilenceMenu(CCSPlayerController admin, CCSPlayerController? player, int duration)
		{
			// TODO: Localize and make options in config?
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_silence"] ?? "Silence"}: {player?.PlayerName}");

			foreach (var option in CSSPanel.Instance.Config.MenuConfigs.MuteReasons)
			{
				menu.AddMenuOption(option, (_, _) =>
				{
					if (player is { IsValid: true })
						Silence(admin, player, duration, option);
				});
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Silence(CCSPlayerController admin, CCSPlayerController? player, int duration, string reason)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Instance.Silence(admin, player, duration, reason);
		}

		private static void ForceTeamMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			// TODO: Localize
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_team_force"] ?? "Force Team"} {player?.PlayerName}");
			List<ChatMenuOptionData> options =
			[
				new ChatMenuOptionData(CSSPanel._localizer?["sa_team_ct"] ?? "CT", () => ForceTeam(admin, player, "ct", CsTeam.CounterTerrorist)),
				new ChatMenuOptionData(CSSPanel._localizer?["sa_team_t"] ?? "T", () => ForceTeam(admin, player, "t", CsTeam.Terrorist)),
				new ChatMenuOptionData(CSSPanel._localizer?["sa_team_swap"] ?? "Swap", () => ForceTeam(admin, player, "swap", CsTeam.Spectator)),
				new ChatMenuOptionData(CSSPanel._localizer?["sa_team_spec"] ?? "Spec", () => ForceTeam(admin, player, "spec", CsTeam.Spectator)),
			];

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void ForceTeam(CCSPlayerController admin, CCSPlayerController? player, string teamName, CsTeam teamNum)
		{
			if (player is not { IsValid: true }) return;
			
			CSSPanel.Instance.ChangeTeam(admin, player, teamName, teamNum, true);
		}
	}
}