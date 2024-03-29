using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using static Dapper.SqlMapper;

namespace CSSPanel;

public partial class CSSPanel
{
	public static HashSet<int> loadedPlayers = new HashSet<int>();

	private void RegisterEvents()
	{
		RegisterListener<Listeners.OnMapStart>(OnMapStart);
		//RegisterListener<Listeners.OnClientConnected>(OnClientConnected); // stats
		//RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect); // stats -> change the seconds amount to the calculated time
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);

		// Chat Log
		AddCommandListener("say", OnPlayerSayPublic, HookMode.Post);
		AddCommandListener("say_team", OnPlayerSayTeam, HookMode.Post);
	}

	bool IsStringValid(string input)
	{
		if (!string.IsNullOrEmpty(input) && !input.Contains($" ") && input.Any(c => Config.ChatLog.ExcludeMessageContains.Contains(c)) && !char.IsWhiteSpace(input.Last()))
		{
			return true;
		}
		return false;
	}

	[GameEventHandler]
	public HookResult OnClientDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

#if DEBUG
		Logger.LogCritical("[OnClientDisconnect] Before");
#endif

		if (player == null || !player.IsValid || string.IsNullOrEmpty(player.IpAddress) || player.IsBot || player.IsHLTV)
		{
			return HookResult.Continue;
		}

		if (!loadedPlayers.Contains(player.Slot))
		{
			return HookResult.Continue;
		}

#if DEBUG
		Logger.LogCritical("[OnClientDisconnect] After Check");
#endif
		try
		{
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();
			playerPenaltyManager.RemoveAllPenalties(player.Slot);

			if (TagsDetected)
			{
				Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");
			}

			if (silentPlayers.Contains(player.Slot))
			{
				RemoveFromConcurrentBag(silentPlayers, player.Slot);
			}

			if (godPlayers.Contains(player.Slot))
			{
				RemoveFromConcurrentBag(godPlayers, player.Slot);
			}

			SteamID? authorizedSteamID = player.AuthorizedSteamID;
			if (authorizedSteamID != null && AdminSQLManager._adminCache.TryGetValue(authorizedSteamID, out DateTime? expirationTime)
				&& expirationTime <= DateTime.Now)
			{
				AdminManager.ClearPlayerPermissions(authorizedSteamID);
				AdminManager.RemovePlayerAdminData(authorizedSteamID);
			}

			loadedPlayers.Remove(player.Slot);

			return HookResult.Continue;
		}
		catch (Exception ex)
		{
			Logger.LogError($"An error occurred in OnClientDisconnect: {ex.Message}");
			return HookResult.Continue;
		}
	}


	[GameEventHandler]
	public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || string.IsNullOrEmpty(player.IpAddress) || player.IpAddress.Contains("127.0.0.1")
			|| player.IsBot || player.IsHLTV || !player.UserId.HasValue)
			return HookResult.Continue;

		string ipAddress = player.IpAddress.Split(":")[0];

		// Check if the player's IP or SteamID is in the bannedPlayers list
		if (bannedPlayers.Contains(ipAddress) || bannedPlayers.Contains(player.SteamID.ToString()))
		{
			// Kick the player if banned
			if (player.UserId.HasValue)
				Helper.KickPlayer(player.UserId.Value, "Banned");

			return HookResult.Continue;
		}

		if (_database == null) return HookResult.Continue;

		PlayerInfo playerInfo = new PlayerInfo
		{
			UserId = player.UserId.Value,
			Index = (ushort)player.Index,
			Slot = player.Slot,
			SteamId = player.SteamID.ToString(),
			Name = player.PlayerName,
			IpAddress = ipAddress
		};

		Task.Run(async () =>
		{
			// Initialize BanManager, MuteManager, and PlayerPenaltyManager within the async delegate
			BanManager _banManager = new(_database, Config);
			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			if (await _banManager.IsPlayerBanned(playerInfo))
			{
				if (playerInfo.IpAddress != null && !bannedPlayers.Contains(playerInfo.IpAddress))
					bannedPlayers.Add(playerInfo.IpAddress);

				if (playerInfo.SteamId != null && !bannedPlayers.Contains(playerInfo.SteamId))
					bannedPlayers.Add(playerInfo.SteamId);

				Server.NextFrame(() =>
				{
					var victim = Utilities.GetPlayerFromUserid(playerInfo.UserId);
					if (victim != null && victim.UserId.HasValue)
					{
						Helper.KickPlayer(victim.UserId.Value, "Banned");
					}
				});

				return;
			}

			List<dynamic> activeMutes = await _muteManager.IsPlayerMuted(playerInfo.SteamId);

			if (activeMutes.Count > 0)
			{
				foreach (dynamic mute in activeMutes)
				{
					string muteType = mute.type;
					DateTime ends = mute.ends;
					int duration = mute.duration;

					if (muteType == "GAG")
					{
						playerPenaltyManager.AddPenalty(playerInfo.Slot, PenaltyType.Gag, ends, duration);
						Server.NextFrame(() =>
						{
							if (TagsDetected)
							{
								Server.ExecuteCommand($"css_tag_mute {playerInfo.SteamId}");
							}
						});
					}
					else if (muteType == "MUTE")
					{
						playerPenaltyManager.AddPenalty(playerInfo.Slot, PenaltyType.Mute, ends, duration);
						Server.NextFrame(() =>
						{
							player.VoiceFlags = VoiceFlags.Muted;
						});
					}
					else
					{
						playerPenaltyManager.AddPenalty(playerInfo.Slot, PenaltyType.Silence, ends, duration);
						Server.NextFrame(() =>
						{
							player.VoiceFlags = VoiceFlags.Muted;
							if (TagsDetected)
							{
								Server.ExecuteCommand($"css_tag_mute {playerInfo.SteamId}");
							}
						});
					}
				}
			}
		});

		// Add player to loadedPlayers
		if (!loadedPlayers.Contains(player.Slot))
			loadedPlayers.Add(player.Slot);

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnRoundEnd(EventRoundStart @event, GameEventInfo info)
	{
#if DEBUG
		Logger.LogCritical("[OnRoundEnd]");
#endif

		godPlayers.Clear();
		return HookResult.Continue;
	}

	public HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).Length == 0 || info.GetArg(1).StartsWith("/")
			 || info.GetArg(1).StartsWith("!") && info.GetArg(1).Length >= 12)
			return HookResult.Continue;

		PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

		if (playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		return HookResult.Continue;
	}

	public HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).Length == 0 || info.GetArg(1).StartsWith("/")
			 || info.GetArg(1).StartsWith("!") && info.GetArg(1).Length >= 12)
			return HookResult.Continue;

		PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

		if (playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		if (info.GetArg(1).StartsWith("@"))
		{
			StringBuilder sb = new();

			if (AdminManager.PlayerHasPermissions(player, "@css/chat"))
			{
				sb.Append(_localizer!["sa_adminchat_template_admin", player!.PlayerName, info.GetArg(1).Remove(0, 1)]);
				foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
				{
					p.PrintToChat(sb.ToString());
				}
			}
			else
			{
				sb.Append(_localizer!["sa_adminchat_template_player", player!.PlayerName, info.GetArg(1).Remove(0, 1)]);
				player.PrintToChat(sb.ToString());
				foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
				{
					p.PrintToChat(sb.ToString());
				}
			}

			return HookResult.Handled;
		}

		return HookResult.Continue;
	}

	private void OnMapStart(string mapName)
	{
		string? path = Path.GetDirectoryName(ModuleDirectory);
		if (Directory.Exists(path + "/CS2-Tags"))
		{
			TagsDetected = true;
		}

		godPlayers.Clear();
		silentPlayers.Clear();

		_logger?.LogInformation("Map started");

		PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();
		playerPenaltyManager.RemoveAllPenalties();

		_database = new(dbConnectionString);

		if (_database == null) return;

		AddTimer(61.0f, () =>
		{

#if DEBUG
			Logger.LogCritical("[OnMapStart] Expired check");
#endif

			Task.Run(async () =>
			{
				AdminSQLManager _adminManager = new AdminSQLManager(_database);
				BanManager _banManager = new BanManager(_database, Config);
				MuteManager _muteManager = new MuteManager(_database);
				await _banManager.ExpireOldBans();
				await _muteManager.ExpireOldMutes();
				await _adminManager.DeleteOldAdmins();

				bannedPlayers.Clear();

				Server.NextFrame(() =>
				{
					try
					{
						foreach (CCSPlayerController player in Helper.GetValidPlayers())
						{
							if (playerPenaltyManager.IsSlotInPenalties(player.Slot))
							{
								if (!playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute) && !playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
									player.VoiceFlags = VoiceFlags.Normal;

								if (!playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) && !playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
								{
									if (TagsDetected)
										Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");
								}

								if (
									!playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence) &&
									!playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute) &&
									!playerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag)
								)
								{
									player.VoiceFlags = VoiceFlags.Normal;

									if (TagsDetected)
										Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");
								}
							}
						}

						playerPenaltyManager.RemoveExpiredPenalties();
					}
					catch (Exception) { }
				});
			});
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

		_logger?.LogInformation("AddTimer");
		AddTimer(2.5f, () =>
		{
			_logger?.LogInformation("Start checking server_id");
			string? address = $"{(Config.DefaultServerIP != "" ? Config.DefaultServerIP : ConVar.Find("ip")!.StringValue)}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";
			string? hostname = ConVar.Find("hostname")!.StringValue;
			string? rcon = ConVar.Find("rcon_password")!.StringValue;

			_logger?.LogInformation($"Server Address: {address}");
			_logger?.LogInformation($"Server Hostname: {hostname}");
			_logger?.LogInformation($"Server Rcon: {rcon}");

			Task.Run(async () =>
			{
				AdminSQLManager _adminManager = new(_database);
				try
				{
					await using var connection = await _database.GetConnectionAsync();
					bool addressExists = await connection.ExecuteScalarAsync<bool>(
						"SELECT COUNT(*) FROM sa_servers WHERE address = @address",
						new { address });

					_logger?.LogInformation($"Server address exists: {addressExists}");

					if (!addressExists)
					{
						_logger?.LogInformation("Server address does not exist, creating new entry");
						await connection.ExecuteAsync(
							"INSERT INTO sa_servers (address, hostname, rcon) VALUES (@address, @hostname, @rcon)",
							new { address, hostname, rcon });
					}
					else
					{
						_logger?.LogInformation("Server address exists, updating entry");
						await connection.ExecuteAsync(
							"UPDATE `sa_servers` SET hostname = @hostname, rcon = @rcon WHERE address = @address",
							new { address, rcon, hostname });
					}

					int? serverId = await connection.ExecuteScalarAsync<int>(
						"SELECT `id` FROM `sa_servers` WHERE `address` = @address",
						new { address });

					if (serverId == null)
					{
						_logger?.LogCritical("Unable to get server_id");
						return;
					}

					_logger?.LogInformation("Server Id: " + serverId);

					ServerId = serverId;
				}
				catch (Exception ex)
				{
					_logger?.LogCritical("Unable to create or get server_id" + ex.Message);
				}

				await _adminManager.GiveAllFlags();
			});

		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

		AddTimer(3.0f, () =>
		{
			ConVar? botQuota = ConVar.Find("bot_quota");

			if (botQuota != null && botQuota.GetPrimitiveValue<int>() > 0)
			{
				Logger.LogWarning("Due to bugs with bots (game bug), consider disabling bots by setting `bot_quota 0` in the gamemode config if your server crashes after a map change.");
			}
		});
	}

	private HookResult OnPlayerSayPublic(CCSPlayerController? player, CommandInfo info)
	{
		if (Config.ChatLog.ChatLog_Enable == false) return HookResult.Continue;
		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;

		bool isTeamChat = false;
		if (player.UserId.HasValue)
		{
			BteamChat[player.UserId.Value] = false;
			isTeamChat = BteamChat[player.UserId.Value];
		}

		var message = info.GetArg(1);

		if (message.StartsWith('/')) return HookResult.Continue;

		if (string.IsNullOrWhiteSpace(message)) return HookResult.Continue;
		string trimmedMessage1 = message.TrimStart();
		string trimmedMessage = trimmedMessage1.TrimEnd();

		if (!string.IsNullOrEmpty(Config.ChatLog.ExcludeMessageContains) && IsStringValid(trimmedMessage)) return HookResult.Continue;
		if (Config.ChatLog.ExcludeMessageContainsLessThanXLetters > 0 && CountLetters(trimmedMessage) <= Config.ChatLog.ExcludeMessageContainsLessThanXLetters)
		{
			return HookResult.Continue;
		}

		var vplayername = player.PlayerName;
		var steamId64 = (player.AuthorizedSteamID != null) ? player.AuthorizedSteamID.SteamId64.ToString() : "InvalidSteamID";

		secondMessage = firstMessage;
		firstMessage = trimmedMessage;

		if (Config.ChatLog.ExcludeMessageDuplicate && secondMessage == firstMessage) return HookResult.Continue;

		// Add to db
		AddChatMessageDB(
			steamId64,
			vplayername,
			trimmedMessage,
			isTeamChat
		);

		return HookResult.Continue;
	}

	private HookResult OnPlayerSayTeam(CCSPlayerController? player, CommandInfo info)
	{
		if (Config.ChatLog.ChatLog_Enable == false) return HookResult.Continue;
		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;

		bool isTeamChat = true;
		if (player.UserId.HasValue)
		{
			BteamChat[player.UserId.Value] = true;
			isTeamChat = BteamChat[player.UserId.Value];
		}

		var message = info.GetArg(1);

		if (message.StartsWith('/')) return HookResult.Continue;

		if (string.IsNullOrWhiteSpace(message)) return HookResult.Continue;
		string trimmedMessage1 = message.TrimStart();
		string trimmedMessage = trimmedMessage1.TrimEnd();

		if (!string.IsNullOrEmpty(Config.ChatLog.ExcludeMessageContains) && IsStringValid(trimmedMessage)) return HookResult.Continue;
		if (Config.ChatLog.ExcludeMessageContainsLessThanXLetters > 0 && CountLetters(trimmedMessage) <= Config.ChatLog.ExcludeMessageContainsLessThanXLetters)
		{
			return HookResult.Continue;
		}

		var vplayername = player.PlayerName;
		var steamId64 = (player.AuthorizedSteamID != null) ? player.AuthorizedSteamID.SteamId64.ToString() : "InvalidSteamID";

		secondMessage = firstMessage;
		firstMessage = trimmedMessage;

		if (Config.ChatLog.ExcludeMessageDuplicate && secondMessage == firstMessage) return HookResult.Continue;

		// Add to db
		AddChatMessageDB(
			steamId64,
			vplayername,
			trimmedMessage,
			isTeamChat
		);

		return HookResult.Continue;
	}

	public void AddChatMessageDB(string playerSteam64, string playerName, string message, bool? team)
	{
		Task.Run(async () =>
		{
			try
			{
				if (_database == null)
					return;
				await using var connection = await _database.GetConnectionAsync();
				var sql = "INSERT INTO `sa_chatlogs` (`playerSteam64`, `playerName`, `message`, `team`, `created`, `serverId`) " +
					"VALUES (@playerSteam64, @playerName, @message, @team, @created, @serverId)";
				int? serverId = ServerId;
				if (serverId == null)
					return;
				DateTime now = DateTime.Now;
				await connection.ExecuteAsync(sql, new
				{
					playerSteam64,
					playerName,
					message,
					team = team ?? null,
					created = now,
					serverid = serverId
				});
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
			}
		});
	}

	[GameEventHandler]
	public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player is null || @event.Attacker is null || !player.PawnIsAlive || player.PlayerPawn.Value == null)
			return HookResult.Continue;

		if (godPlayers.Contains(player.Slot))
		{
			player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
			player.PlayerPawn.Value.ArmorValue = 100;
		}

		return HookResult.Continue;
	}
}