using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CSSPanel.Menus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CSSPanel
{
	public partial class CSSPanel
	{
		[ConsoleCommand("css_sa_upgrade")]
		[CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
		public void OnSaUpgradeCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller != null || _database == null) return;

			Task.Run(async () =>
			{
				try
				{
					List<string> upgradeMySQLCommands = new()
					{
						"ALTER TABLE `sa_mutes` CHANGE `type` `type` ENUM('GAG','MUTE', 'SILENCE', '') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'GAG';",
						"ALTER TABLE `sa_servers` MODIFY COLUMN `hostname` varchar(128);",
						"ALTER TABLE `sa_bans` MODIFY `ends` TIMESTAMP NULL DEFAULT NULL;",
						"ALTER TABLE `sa_statistics` DROP `created`;",
						"ALTER TABLE `sa_admins` CHANGE `server_id` `server_id` VARCHAR(50) NULL DEFAULT NULL;",
						"ALTER TABLE `sa_servers` ADD COLUMN IF NOT EXISTS `rcon` VARCHAR(64) NULL;"
					};

					using var connection = await _database.GetConnectionAsync();

					foreach (string upgradeCommand in upgradeMySQLCommands)
					{
						try
						{
							using var commandSql = connection.CreateCommand();
							commandSql.CommandText = upgradeCommand;
							await commandSql.ExecuteNonQueryAsync();
						}
						catch (Exception ex)
						{
							Logger.LogError($"[UPGRADE] Error while executing mysql command: {upgradeCommand} {ex.Message}");
						}
					}

					Server.NextFrame(() =>
					{
						command.ReplyToCommand($"Successfully updated the database - {ModuleVersion}");
					});
				}
				catch (Exception ex)
				{
					Logger.LogError(ex.Message);
				}
			});
		}

		[ConsoleCommand("css_admin")]
		[RequiresPermissions("@css/generic")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
		public void OnAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null || caller.IsValid == false)
				return;

			AdminMenu.OpenMenu(caller);
		}

		[ConsoleCommand("css_adminhelp")]
		[RequiresPermissions("@css/generic")]
		public void OnAdminHelpCommand(CCSPlayerController? caller, CommandInfo command)
		{
			//if (caller == null ||!caller.IsValid) return;

			/*
			using (new WithTemporaryCulture(caller.GetLanguage()))
			{
				var splitMessage = _localizer!["sa_adminhelp"].ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

				foreach (var line in splitMessage)
				{
					caller.PrintToChat(Helper.ReplaceTags($" {line}"));
				}
			} */

			var lines = File.ReadAllLines(ModuleDirectory + "/admin_help.txt");

			foreach (var line in lines)
			{
				command.ReplyToCommand(string.IsNullOrWhiteSpace(line) ? " " : line.ReplaceColorTags());
			}
		}

		[ConsoleCommand("css_addadmin")]
		[CommandHelper(minArgs: 4, usage: "<steamid> <name> <flags/groups> <immunity> <duration>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnAddAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;


			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var steamid = steamId.SteamId64.ToString();

			if (command.GetArg(2).Length <= 0)
			{
				command.ReplyToCommand($"Invalid player name.");
				return;
			}
			if (!command.GetArg(3).Contains('@') && !command.GetArg(3).Contains('#'))
			{
				command.ReplyToCommand($"Invalid flag or group.");
				return;
			}

			var name = command.GetArg(2);
			var flags = command.GetArg(3);
			var globalAdmin = command.GetArg(4).ToLower().Equals("-g") || command.GetArg(5).ToLower().Equals("-g") ||
					  command.GetArg(6).ToLower().Equals("-g");
			int.TryParse(command.GetArg(4), out var immunity);
			int.TryParse(command.GetArg(5), out var time);

			AddAdmin(caller, steamid, name, flags, immunity, time, globalAdmin, command);
		}

		public static void AddAdmin(CCSPlayerController? caller, string steamid, string name, string flags, int immunity, int time = 0, bool globalAdmin = false, CommandInfo? command = null)
		{
			if (_database == null) return;
			PermissionManager adminManager = new(_database);

			var flagsList = flags.Split(',').Select(flag => flag.Trim()).ToList();
			_ = adminManager.AddAdminBySteamId(steamid, name, flagsList, immunity, time, globalAdmin);

			if (command != null)
				Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, $"css_addadmin {steamid} {name} {flags} {immunity} {time}");

			var msg = $"Added '{flags}' flags to '{name}' ({steamid})";
			if (command != null)
				command.ReplyToCommand(msg);
			else if (caller != null && caller.IsValid)
				caller.PrintToChat(msg);
			else
				Server.PrintToConsole(msg);
		}

		[ConsoleCommand("css_deladmin")]
		[CommandHelper(minArgs: 1, usage: "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnDelAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var globalDelete = command.GetArg(2).ToLower().Equals("-g");

			RemoveAdmin(caller, steamId.SteamId64.ToString(), globalDelete, command);
		}

		public void RemoveAdmin(CCSPlayerController? caller, string steamid, bool globalDelete = false, CommandInfo? command = null)
		{
			if (_database == null) return;
			PermissionManager adminManager = new(_database);
			_ = adminManager.DeleteAdminBySteamId(steamid, globalDelete);

			AddTimer(2, () =>
			{
				if (string.IsNullOrEmpty(steamid) || !SteamID.TryParse(steamid, out var steamId) ||
					steamId == null) return;
				if (PermissionManager.AdminCache.ContainsKey(steamId))
				{
					PermissionManager.AdminCache.TryRemove(steamId, out _);
				}

				AdminManager.ClearPlayerPermissions(steamId);
				AdminManager.RemovePlayerAdminData(steamId);
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			if (command != null)
				Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, $"css_deladmin {steamid}");

			var msg = $"Removed flags from '{steamid}'";
			if (command != null)
				command.ReplyToCommand(msg);
			else if (caller != null && caller.IsValid)
				caller.PrintToChat(msg);
			else
				Server.PrintToConsole(msg);
		}

		[ConsoleCommand("css_reloadadmins")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnRelAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			ReloadAdmins(caller);

			command.ReplyToCommand("Reloaded sql admins and groups");
		}

		public void ReloadAdmins(CCSPlayerController? caller)
		{
			if (_database == null) return;

			foreach (SteamID steamId in PermissionManager.AdminCache.Keys.ToList())
			{
				if (PermissionManager.AdminCache.TryRemove(steamId, out _))
				{
					AdminManager.ClearPlayerPermissions(steamId);
					AdminManager.RemovePlayerAdminData(steamId);
				}
			}

			PermissionManager _adminManager = new(_database);
			_ = _adminManager.GiveAllFlags();
		}

		[ConsoleCommand("css_stealth")]
		[ConsoleCommand("css_hide")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
		[RequiresPermissions("@css/kick")]
		public void OnHideCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null) return;

			Helper.LogCommand(caller, command);

			if (SilentPlayers.Contains(caller.Slot))
			{
				SilentPlayers.Remove(caller.Slot);
				caller.PrintToChat($"You aren't hidden now!");
				caller.ChangeTeam(CsTeam.Spectator);
			}
			else
			{
				SilentPlayers.Add(caller.Slot);
				Server.ExecuteCommand("sv_disable_teamselect_menu 1");

				if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
					caller.PlayerPawn.Value.CommitSuicide(true, false);

				AddTimer(1.0f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.Spectator)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				AddTimer(1.4f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.None)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				caller.PrintToChat($"You are hidden now!");
				AddTimer(2.0f, () => { Server.NextFrame(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
		}

		[ConsoleCommand("css_who")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/generic")]
		public void OnWhoCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			var targets = GetTarget(command);
			if (targets == null) return;

			Helper.LogCommand(caller, command);
			//Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Database.Database database = new(_dbConnectionString);
			BanManager banManager = new(database, Config);
			MuteManager muteManager = new(_database);

			playersToTarget.ForEach(player =>
			{
				if (!player.UserId.HasValue) return;
				if (!caller!.CanTarget(player)) return;
				PlayerInfo playerInfo = new()
				{
					UserId = player.UserId.Value,
					SteamId = player.SteamID.ToString(),
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};

				Task.Run(async () =>
				{
					var totalBans = await banManager.GetPlayerBans(playerInfo);
					var totalMutes = await muteManager.GetPlayerMutes(playerInfo.SteamId);

					await Server.NextFrameAsync(() =>
					{
						Action<string> printMethod = caller == null ? Server.PrintToConsole : caller.PrintToConsole;

						printMethod($"--------- INFO ABOUT \"{playerInfo.Name}\" ---------");

						printMethod($"• Clan: \"{player.Clan}\" Name: \"{playerInfo.Name}\"");
						printMethod($"• UserID: \"{playerInfo.UserId}\"");
						if (playerInfo.SteamId != null)
							printMethod($"• SteamID64: \"{playerInfo.SteamId}\"");
						if (player.Connected == PlayerConnectedState.PlayerConnected)
						{
							printMethod($"• SteamID2: \"{player.SteamID}\"");
							printMethod($"• Community link: \"{new SteamID(player.SteamID).ToCommunityUrl()}\"");
						}
						if (playerInfo.IpAddress != null && AdminManager.PlayerHasPermissions(caller, "@css/showip"))
							printMethod($"• IP Address: \"{playerInfo.IpAddress}\"");
						printMethod($"• Ping: \"{player.Ping}\"");
						if (player.Connected == PlayerConnectedState.PlayerConnected)
						{
							printMethod($"• Total Bans: \"{totalBans}\"");
							printMethod($"• Total Mutes: \"{totalMutes}\"");
						}

						printMethod($"--------- END INFO ABOUT \"{player.PlayerName}\" ---------");
					});
				});
			});
		}

		[ConsoleCommand("css_players")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/generic")]
		public void OnPlayersCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var isJson = command.GetArg(1).ToLower().Equals("-json");
			var isDuplicate = command.GetArg(1).ToLower().Equals("-duplicate") || command.GetArg(2).ToLower().Equals("-duplicate");

			var playersToTarget = isDuplicate
				? Helper.GetValidPlayers().GroupBy(player => player.IpAddress?.Split(":")[0] ?? "Unknown")
										  .Where(group => group.Count() > 1)
										  .SelectMany(group => group)
										  .ToList()
				: Helper.GetValidPlayers();

			if (!isJson)
			{
				if (caller != null)
				{
					caller.PrintToConsole($"--------- PLAYER LIST ---------");
					playersToTarget.ForEach(player =>
					{
						caller.PrintToConsole(
							$"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{(AdminManager.PlayerHasPermissions(caller, "@css/showip") ? player.IpAddress?.Split(":")[0] : "Unknown")}\" SteamID64: \"{player.SteamID}\")");
					});
					caller.PrintToConsole($"--------- END PLAYER LIST ---------");
				}
				else
				{
					Server.PrintToConsole($"--------- PLAYER LIST ---------");
					playersToTarget.ForEach(player =>
					{
						Server.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.SteamID}\")");
					});
					Server.PrintToConsole($"--------- END PLAYER LIST ---------");
				}
			}
			else
			{
				var playersJson = JsonConvert.SerializeObject(playersToTarget.Select((CCSPlayerController player) =>
				{
					var matchStats = player.ActionTrackingServices?.MatchStats;

					return new
					{
						player.UserId,
						Name = player.PlayerName,
						SteamId = player.SteamID.ToString(),
						IpAddress = AdminManager.PlayerHasPermissions(caller, "@css/showip") ? player.IpAddress?.Split(":")[0] ?? "Unknown" : "Unknown",
						player.Ping,
						IsAdmin = AdminManager.PlayerHasPermissions(player, "@css/ban") || AdminManager.PlayerHasPermissions(player, "@css/generic"),
						Stats = new
						{
							player.Score,
							Kills = matchStats?.Kills ?? 0,
							Deaths = matchStats?.Deaths ?? 0,
							player.MVPs
						}
					};
				}));

				if (caller != null)
					caller.PrintToConsole(playersJson);
				else
					Server.PrintToConsole(playersJson);
			}
		}

		[ConsoleCommand("css_kick")]
		[RequiresPermissions("@css/kick")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Server" : caller.PlayerName;
			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			var targets = GetTarget(command);

			if (targets == null) return;
			var playersToTarget = targets.Players
				.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (command.ArgCount >= 2 && command.GetArg(2).Length > 0)
				reason = command.GetArg(2);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsValid)
					return;

				if (caller!.CanTarget(player))
				{
					Kick(caller, player, reason, callerName, command);
				}
			});
		}

		public void Kick(CCSPlayerController? caller, CCSPlayerController? player, string? reason = "Unknown", string? callerName = null, CommandInfo? command = null)
		{
			if (player == null || !player.IsValid) return;
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Server" : caller.PlayerName;
			reason ??= _localizer?["sa_unknown"] ?? "Unknown";

			player.Pawn.Value!.Freeze();

			if (command != null)
				Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, $"css_kick {player?.PlayerName} {reason}");

			if (string.IsNullOrEmpty(reason) == false)
			{
				if (player != null && !player.IsBot)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_kick_message", reason, Config.ShowAdminName ? callerName : "Server"]);
					}
				if (player != null && player.UserId.HasValue)
					AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value, reason),
						CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
			else
			{
				if (player != null && player.UserId.HasValue)
					AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value),
						CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}

			if (caller != null && (caller.UserId == null || SilentPlayers.Contains(caller.Slot))) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_kick_message",
										callerName,
										player?.PlayerName ?? string.Empty,
										reason);
			}
		}

		[ConsoleCommand("css_changemap")]
		[ConsoleCommand("css_map")]
		[RequiresPermissions("@css/changemap")]
		[CommandHelper(minArgs: 1, usage: "<mapname>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var map = command.GetCommandString.Split(" ")[1];
			ChangeMap(caller, map, command);
		}

		public void ChangeMap(CCSPlayerController? caller, string map, CommandInfo? command = null)
		{
			map = map.ToLower();

			if (map.StartsWith("ws:"))
			{
				var issuedCommand = long.TryParse(map.Replace("ws:", ""), out var mapId)
					? $"host_workshop_map {mapId}"
					: $"ds_workshop_changelevel {map.Replace("ws:", "")}";

				AddTimer(3.0f, () =>
				{
					Server.ExecuteCommand(issuedCommand);
				}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
			else
			{
				if (!Server.IsMapValid(map))
				{
					var msg = $"Map {map} not found.";
					if (command != null)
						command.ReplyToCommand(msg);
					else if (caller != null && caller.IsValid)
						caller.PrintToChat(msg);
					else
						Server.PrintToConsole(msg);
					return;
				}

				AddTimer(3.0f, () =>
				{
					Server.ExecuteCommand($"changelevel {map}");
				}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}

			if (caller == null || !SilentPlayers.Contains(caller.Slot))
			{
				foreach (var player in Helper.GetValidPlayers())
				{
					if (_localizer != null)
						player.SendLocalizedMessage(_localizer,
											"sa_admin_changemap_message",
											caller == null ? "Server" : caller.PlayerName,
											map);
				}
			}

			if (command == null) return;
			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);
		}

		[ConsoleCommand("css_changewsmap", "Change workshop map.")]
		[ConsoleCommand("css_wsmap", "Change workshop map.")]
		[ConsoleCommand("css_workshop", "Change workshop map.")]
		[CommandHelper(1, "<name or id>")]
		[RequiresPermissions("@css/changemap")]
		public void OnWorkshopMapCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var map = command.GetArg(1);
			ChangeWorkshopMap(caller, map, command);
		}

		public void ChangeWorkshopMap(CCSPlayerController? caller, string map, CommandInfo? command = null)
		{
			map = map.ToLower();

			var issuedCommand = long.TryParse(map, out var mapId) ? $"host_workshop_map {mapId}" : $"ds_workshop_changelevel {map}";

			if (caller == null || !SilentPlayers.Contains(caller.Slot))
			{
				foreach (var player in Helper.GetValidPlayers())
				{
					if (_localizer != null)
						player.SendLocalizedMessage(_localizer,
											"sa_admin_changemap_message",
											caller == null ? "Server" : caller.PlayerName,
											map);
				}
			}

			AddTimer(3.0f, () =>
			{
				Server.ExecuteCommand(issuedCommand);
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			if (command == null) return;
			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);
		}

		[ConsoleCommand("css_cvar", "Change a cvar.")]
		[CommandHelper(2, "<cvar> <value>")]
		[RequiresPermissions("@css/cvar")]
		public void OnCvarCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var cvar = ConVar.Find(command.GetArg(1));
			var callerName = caller == null ? "Server" : caller.PlayerName;

			if (cvar == null)
			{
				command.ReplyToCommand($"Cvar \"{command.GetArg(1)}\" not found.");
				return;
			}

			if (cvar.Name.Equals("sv_cheats") && !AdminManager.PlayerHasPermissions(caller, "@css/cheats"))
			{
				command.ReplyToCommand($"You don't have permissions to change \"{command.GetArg(1)}\".");
				return;
			}

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			var value = command.GetArg(2);

			Server.ExecuteCommand($"{cvar.Name} {value}");

			command.ReplyToCommand($"{callerName} changed cvar {cvar.Name} to {value}.");
			Logger.LogInformation($"{callerName} changed cvar {cvar.Name} to {value}.");
		}

		[ConsoleCommand("css_rcon", "Run a server console command.")]
		[CommandHelper(1, "<command>")]
		[RequiresPermissions("@css/rcon")]
		public void OnRconCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Server" : caller.PlayerName;

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			Server.ExecuteCommand(command.ArgString);
			command.ReplyToCommand($"{callerName} executed command {command.ArgString}.");
			Logger.LogInformation($"{callerName} executed command ({command.ArgString}).");
		}

		[ConsoleCommand("css_rr")]
		[ConsoleCommand("css_rg")]
		[ConsoleCommand("css_restart")]
		[ConsoleCommand("css_restartgame")]
		[RequiresPermissions("@css/generic")]
		[CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnRestartCommand(CCSPlayerController? caller, CommandInfo command)
		{
			RestartGame(caller);
		}

		public static void RestartGame(CCSPlayerController? admin)
		{
			Helper.LogCommand(admin, "css_restartgame");

			// TODO: Localize
			var name = admin == null ? "Server" : admin.PlayerName;
			Server.PrintToChatAll($"[SA] {name}: Restarting game...");
			Server.ExecuteCommand("mp_restartgame 2");
		}
	}
}