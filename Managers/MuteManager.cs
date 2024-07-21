﻿using Dapper;
using Microsoft.Extensions.Logging;

namespace CSSPanel;

internal class MuteManager(Database.Database database)
{
	public async Task MutePlayer(PlayerInfo player, PlayerInfo issuer, string reason, int time = 0, int type = 0)
	{
		if (player.SteamId == null) return;

		var now = DateTime.UtcNow.ToLocalTime();
		var futureTime = now.AddMinutes(time);

		var muteType = type switch
		{
			1 => "MUTE",
			2 => "SILENCE",
			_ => "GAG"
		};

		try
		{
			await using var connection = await database.GetConnectionAsync();
			const string sql =
				"INSERT INTO `sa_mutes` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) " +
							   "VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.SteamId,
				playerName = player.Name,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.SteamId == null ? "Console" : issuer.Name,
				muteReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
				serverid = CSSPanel.ServerId
			});
		}
		catch { };
	}

	public async Task AddMuteBySteamid(string playerSteamId, PlayerInfo issuer, string reason, int time = 0, int type = 0)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;


		var now = DateTime.UtcNow.ToLocalTime();
		var futureTime = now.AddMinutes(time);

		var muteType = type switch
		{
			1 => "MUTE",
			2 => "SILENCE",
			_ => "GAG"
		};

		try
		{
			await using var connection = await database.GetConnectionAsync();
			const string sql = "INSERT INTO `sa_mutes` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) " +
							   "VALUES (@playerSteamid, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.Name ?? "Console",
				muteReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
				serverid = CSSPanel.ServerId
			});
		}
		catch { };
	}

	public async Task<List<dynamic>> IsPlayerMuted(string steamId)
	{
		if (string.IsNullOrEmpty(steamId))
		{
			return [];
		}

#if DEBUG
		if (CSSPanel._logger!= null)
			CSSPanel._logger.LogCritical($"IsPlayerMuted for {steamId}");
#endif

		try
		{
			await using var connection = await database.GetConnectionAsync();
			var currentTime = DateTime.UtcNow.ToLocalTime();
			var sql = "";

			if (CSSPanel.Instance.Config.MultiServerMode)
			{
				sql = CSSPanel.Instance.Config.TimeMode == 1
					? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)"
					: "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR duration > COALESCE(passed, 0))";
			}
			else
			{
				sql = CSSPanel.Instance.Config.TimeMode == 1
					? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime) AND server_id = @serverid"
					: "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR duration > COALESCE(passed, 0)) AND server_id = @serverid";

			}

			var parameters = new { PlayerSteamID = steamId, CurrentTime = currentTime, serverid = CSSPanel.ServerId };
			var activeMutes = (await connection.QueryAsync(sql, parameters)).ToList();
			return activeMutes;
		}
		catch (Exception)
		{
			return [];
		}
	}

	public async Task<int> GetPlayerMutes(string steamId)
	{
		try
		{
			await using var connection = await database.GetConnectionAsync();

			var sql = CSSPanel.Instance.Config.MultiServerMode
				? "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @PlayerSteamID"
				: "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND server_id = @serverid";

			var muteCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId, serverid = CSSPanel.ServerId });
			return muteCount;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	public async Task CheckOnlineModeMutes(List<(string? IpAddress, ulong SteamID, int? UserId, int Slot)> players)
	{
		try
		{
			int batchSize = 10;
			await using var connection = await database.GetConnectionAsync();

			var sql = CSSPanel.Instance.Config.MultiServerMode
				? "UPDATE `sa_mutes` SET passed = COALESCE(passed, 0) + 1 WHERE (player_steamid = @PlayerSteamID) AND duration > 0 AND status = 'ACTIVE'"
				: "UPDATE `sa_mutes` SET passed = COALESCE(passed, 0) + 1 WHERE (player_steamid = @PlayerSteamID) AND duration > 0 AND status = 'ACTIVE' AND server_id = @serverid";
			/*
			foreach (var (IpAddress, SteamID, UserId, Slot) in players)
			{
				await connection.ExecuteAsync(sql,
					new { PlayerSteamID = SteamID, serverid = CSSPanel.ServerId });
			}*/

			for (var i = 0; i < players.Count; i += batchSize)
			{
				var batch = players.Skip(i).Take(batchSize);
				var parametersList = new List<object>();

				foreach (var (IpAddress, SteamID, UserId, Slot) in batch)
				{
					parametersList.Add(new { PlayerSteamID = SteamID, serverid = CSSPanel.ServerId });
				}

				await connection.ExecuteAsync(sql, parametersList);
			}

			sql = CSSPanel.Instance.Config.MultiServerMode
				? "SELECT * FROM `sa_mutes` WHERE player_steamid = @PlayerSteamID AND passed >= duration AND duration > 0 AND status = 'ACTIVE'"
				: "SELECT * FROM `sa_mutes` WHERE player_steamid = @PlayerSteamID AND passed >= duration AND duration > 0 AND status = 'ACTIVE' AND server_id = @serverid";


			foreach (var (IpAddress, SteamID, UserId, Slot) in players)
			{
				var muteRecords = await connection.QueryAsync(sql, new { PlayerSteamID = SteamID, serverid = CSSPanel.ServerId });

				foreach (var muteRecord in muteRecords)
				{
					DateTime endDateTime = muteRecord.ends;
					PlayerPenaltyManager.RemovePenaltiesByDateTime(Slot, endDateTime);
				}

			}
		}
		catch { }
	}

	public async Task UnmutePlayer(string playerPattern, string adminSteamId, string reason, int type = 0)
	{
		if (playerPattern.Length <= 1)
		{
			return;
		}

		try
		{
			await using var connection = await database.GetConnectionAsync();

			var muteType = type switch
			{
				1 => "MUTE",
				2 => "SILENCE",
				_ => "GAG"
			};

			string sqlRetrieveMutes;

			if (CSSPanel.Instance.Config.MultiServerMode)
			{
				sqlRetrieveMutes = "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND " +
					"type = @muteType AND status = 'ACTIVE'";
			}
			else
			{
				sqlRetrieveMutes = "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND " +
					"type = @muteType AND status = 'ACTIVE' AND server_id = @serverid";
			}

			var mutes = await connection.QueryAsync(sqlRetrieveMutes, new { pattern = playerPattern, muteType, serverid = CSSPanel.ServerId });

			var mutesList = mutes as dynamic[] ?? mutes.ToArray();
			if (mutesList.Length == 0)
				return;

			const string sqlAdmin = "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";
			var sqlInsertUnmute = "INSERT INTO sa_unmutes (mute_id, admin_id, reason) VALUES (@muteId, @adminId, @reason); SELECT LAST_INSERT_ID();";

			var sqlAdminId = await connection.ExecuteScalarAsync<int?>(sqlAdmin, new { adminSteamId });
			var adminId = sqlAdminId ?? 0;

			foreach (var mute in mutesList)
			{
				int muteId = mute.id;
				int? unmuteId;

				// Insert into sa_unmutes
				if (reason != null)
				{
					unmuteId = await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId, reason });
				}
				else
				{
					sqlInsertUnmute = "INSERT INTO sa_unmutes (muteId, admin_id) VALUES (@muteId, @adminId); SELECT LAST_INSERT_ID();";
					unmuteId = await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId });
				}

				// Update sa_mutes to set unmute_id
				const string sqlUpdateMute = "UPDATE sa_mutes SET status = 'UNMUTED', unmute_id = @unmuteId WHERE id = @muteId";
				await connection.ExecuteAsync(sqlUpdateMute, new { unmuteId, muteId });
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}

	public async Task ExpireOldMutes()
	{
		try
		{
			await using var connection = await database.GetConnectionAsync();
			var sql = "";

			if (CSSPanel.Instance.Config.MultiServerMode)
			{
				sql = CSSPanel.Instance.Config.TimeMode == 1
					? "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime"
					: "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND `passed` >= `duration`";
			}
			else
			{
				sql = CSSPanel.Instance.Config.TimeMode == 1
					? "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime AND server_id = @serverid"
					: "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND `passed` >= `duration` AND server_id = @serverid";
			}

			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.UtcNow.ToLocalTime(), serverid = CSSPanel.ServerId });
		}
		catch (Exception)
		{
			CSSPanel._logger?.LogCritical("Unable to remove expired mutes");
		}
	}
}