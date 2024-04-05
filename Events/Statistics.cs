using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CSSPanel;

public partial class CSSPanel
{
	[GameEventHandler]
	public HookResult OnPlayerConnectStatistics(EventPlayerConnectFull @event, GameEventInfo info)
	{
		if (Config.Statistics.Statistics_Enable == false) return HookResult.Continue;

		CCSPlayerController? player = @event.Userid;

		if (player == null || string.IsNullOrEmpty(player.IpAddress) || player.IpAddress.Contains("127.0.0.1")
			|| player.IsBot || player.IsHLTV || !player.UserId.HasValue || player.AuthorizedSteamID == null)
		{

			return HookResult.Continue;
		}


		if (_database == null) return HookResult.Continue;

		SteamID authorizedSteamID = player.AuthorizedSteamID!;
		string ipAddress = player.IpAddress!.Split(":")[0];
		string playerId = authorizedSteamID.SteamId64.ToString();
		string playerName = player.PlayerName;
		AdminSQLManager _adminManager = new(_database);

		// Unix timestamp
		long Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		string Map = Server.MapName;

		if (ServerId == null)
			return HookResult.Continue;

		Task.Run(async () =>
		{
			try
			{
				await using var connection = await _database.GetConnectionAsync();

				string sql = "INSERT INTO `sa_statistics` (`serverId`, `playerId`, `playerName`, `playerIP`, `connectTime`, `flags`, `map`) " +
					"VALUES (@serverId, @playerId, @playerName, @playerIP, @connectTime, @flags, @map)";

				List<string> adminFlags = await _adminManager.GetAdminFlagsAsString(playerId);

				await connection.ExecuteAsync(sql, new
				{
					serverId = ServerId,
					playerId,
					playerName,
					playerIP = ipAddress,
					connectTime = Time,
					flags = adminFlags.Count > 0 ? string.Join(",", adminFlags) : null,
					map = Map
				});
			}
			catch (Exception ex)
			{
				Logger.LogError($"An error occurred in OnPlayerConnect: {ex.Message}");
			}
		});

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnClientDisconnectStatistics(EventPlayerDisconnect @event, GameEventInfo info)
	{
		if (Config.Statistics.Statistics_Enable == false) return HookResult.Continue;

		CCSPlayerController? player = @event.Userid;

		if (player == null || string.IsNullOrEmpty(player.IpAddress) || player.IpAddress.Contains("127.0.0.1")
			|| player.IsBot || player.IsHLTV || !player.UserId.HasValue || player.AuthorizedSteamID == null)
			return HookResult.Continue;

		if (_database == null) return HookResult.Continue;

		SteamID authorizedSteamID = player.AuthorizedSteamID!;
		string ipAddress = player.IpAddress!.Split(":")[0];
		string playerId = authorizedSteamID.SteamId64.ToString();
		string playerName = player.PlayerName;
		AdminSQLManager _adminManager = new(_database);
		DateTime now = DateTime.Now;

		// Unix timestamp
		long Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		string kills = player.ActionTrackingServices!.MatchStats.Kills.ToString();

		Task.Run(async () =>
		{
			try
			{
				await using var connection = await _database.GetConnectionAsync();

				string sql = "UPDATE `sa_statistics` SET `disconnectTime` = @disconnectTime, `disconnectDate` = @disconnectDate, `kills` = @kills, `duration` = @disconnectTime - `connectTime`  " +
					"WHERE `playerId` = @playerId AND `disconnectTime` IS NULL";

				await connection.ExecuteAsync(sql, new
				{
					disconnectTime = Time,
					disconnectDate = now,
					kills,
					playerId,
				});
			}
			catch (Exception ex)
			{
				Logger.LogError($"An error occurred in OnClientDisconnectStatistics: {ex.Message}");
			}
		});

		return HookResult.Continue;

	}
}
