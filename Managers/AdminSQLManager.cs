using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CSSPanel;

public class AdminSQLManager
{
	private readonly Database _database;

	// Unused for now
	//public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
	public static readonly ConcurrentDictionary<SteamID, DateTime?> _adminCache = new ConcurrentDictionary<SteamID, DateTime?>();

	//public static readonly ConcurrentDictionary<SteamID, DateTime?> _adminCacheTimestamps = new ConcurrentDictionary<SteamID, DateTime?>();

	public AdminSQLManager(Database database)
	{
		_database = database;
	}

	// Get the relevant server groups from the sa_servers_groups table by searching if the serverId is in the servers set column
	public async Task<List<string>> GetServerGroups()
	{
		await using MySqlConnection connection = await _database.GetConnectionAsync();

		string sql = "SELECT id FROM sa_servers_groups WHERE FIND_IN_SET(@ServerId, servers)";
		List<dynamic>? serverGroups = (await connection.QueryAsync(sql, new { ServerId = CSSPanel.ServerId }))?.ToList();

		if (serverGroups == null)
		{
			return new List<string>();
		}

		List<string> serverGroupsList = new List<string>();

		foreach (dynamic serverGroup in serverGroups)
		{
			if (serverGroup is not IDictionary<string, object> serverGroupDict)
			{
				Console.WriteLine("[GetServerGroups] Failed to parse server group.");
				continue;
			}

			if (!serverGroupDict.TryGetValue("id", out var idObj))
			{
				Console.WriteLine("[GetServerGroups] Failed to get server group id.");
				continue;
			}

			if (idObj is not string id)
			{
				// Convert to string and add it to the list
				// Console.WriteLine("[GetServerGroups] Failed to parse server group id.");
				serverGroupsList.Add(idObj.ToString()!);

				continue;
			}

			serverGroupsList.Add(id);
		}

		Console.WriteLine($"[GetServerGroups] Server Groups List: {string.Join(", ", serverGroupsList)}");
		return serverGroupsList;
	}

	public async Task<List<string>> GetAdminFlagsAsString(string steamId)
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();

		await using MySqlConnection connection = await _database.GetConnectionAsync();

		// string sql = "SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR FIND_IN_SET(@serverid, server_id) > 0)";
		// string sql = "SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR FIND_IN_SET(@serverid, server_id) > 0)";
		List<string> serverGroups = await GetServerGroups();
		string serverGroupsCondition = string.Join(" OR ", serverGroups.Select(g => $"FIND_IN_SET({g}, servers_groups) > 0"));

		string GroupCheck = serverGroups.Count > 0 ? $"AND (((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0) OR (servers_groups IS NULL OR {serverGroupsCondition}))" : "AND ((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0)";
		string sql = $"SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) {GroupCheck}";

		List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now, serverid = CSSPanel.ServerId }))?.ToList();

		if (activeFlags == null)
		{
			return new List<string>();
		}

		List<string> flagsList = new List<string>();

		foreach (dynamic flags in activeFlags)
		{
			if (flags is not IDictionary<string, object> flagsDict)
			{
				continue;
			}

			if (!flagsDict.TryGetValue("flags", out var flagsValueObj))
			{
				continue;
			}

			if (!(flagsValueObj is string flagsValue))
			{
				continue;
			}

			// If flags start with '#', fetch flags from sa_admins_groups
			if (flagsValue.StartsWith("#"))
			{
				string groupSql = "SELECT flags FROM sa_admins_groups WHERE id = @GroupId";
				var group = await connection.QueryFirstOrDefaultAsync(groupSql, new { GroupId = flagsValue });

				if (group != null)
				{
					flagsValue = group.flags;
				}
			}

			flagsList.AddRange(flagsValue.Split(','));
		}

		return flagsList;
	}

	public async Task<List<(string, List<string>, int, DateTime?)>> GetAllPlayersFlags()
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			// string sql = "SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR FIND_IN_SET(@serverid, server_id) > 0)";
			List<string> serverGroups = await GetServerGroups();
			string serverGroupsCondition = string.Join(" OR ", serverGroups.Select(g => $"FIND_IN_SET({g}, servers_groups) > 0"));

			string GroupCheck = serverGroups.Count > 0 ? $"AND (((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0) OR (servers_groups IS NULL OR {serverGroupsCondition}))" : "AND ((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0)";
			string sql = $"SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) {GroupCheck}";

			List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CSSPanel.ServerId }))?.ToList();

			if (activeFlags == null)
			{
				return new List<(string, List<string>, int, DateTime?)>();
			}

			List<(string, List<string>, int, DateTime?)> filteredFlagsWithImmunity = new List<(string, List<string>, int, DateTime?)>();

			foreach (dynamic flags in activeFlags)
			{
				if (flags is not IDictionary<string, object> flagsDict)
				{
					continue;
				}

				if (!flagsDict.TryGetValue("player_steamid", out var steamIdObj) ||
					!flagsDict.TryGetValue("flags", out var flagsValueObj) ||
					!flagsDict.TryGetValue("immunity", out var immunityValueObj) ||
					!flagsDict.TryGetValue("ends", out var endsObj))
				{
					//Console.WriteLine("One or more required keys are missing.");
					continue;
				}

				DateTime? ends = null;

				if (endsObj != null) // Check if "ends" is not null
				{
					if (!DateTime.TryParse(endsObj.ToString(), out var parsedEnds))
					{
						//Console.WriteLine("Failed to parse 'ends' value.");
						continue;
					}

					ends = parsedEnds;
				}

				if (!(steamIdObj is string steamId) ||
					!(flagsValueObj is string flagsValue) ||
					!int.TryParse(immunityValueObj.ToString(), out var immunityValue))
				{
					//Console.WriteLine("Failed to parse one or more values.");
					continue;
				}

				//
				if (flagsValue.StartsWith("#"))
				{
					string groupSql = "SELECT flags, immunity FROM sa_admins_groups WHERE id = @GroupId";
					var group = await connection.QueryFirstOrDefaultAsync(groupSql, new { GroupId = flagsValue });

					if (group != null)
					{
						flagsValue = group.flags;
						if (int.TryParse(group.immunity.ToString(), out int immunityGroupValue))
						{
							filteredFlagsWithImmunity.Add((steamId, flagsValue.Split(',').ToList(), immunityGroupValue, ends));
							// Console.WriteLine($"Flags Check (Group): SteamId {steamId} Flags: {flagsValue}, Immunity: {immunityValue}");

							continue;
						}
						else
						{
							Console.WriteLine($"Failed to parse immunity: {group.immunity}");
						}
					}
				}

				filteredFlagsWithImmunity.Add((steamId, flagsValue.Split(',').ToList(), immunityValue, ends));
			}

			return filteredFlagsWithImmunity;
		}
		catch (Exception e)
		{
			Console.WriteLine($"Error: {e.Message}");
			return new List<(string, List<string>, int, DateTime?)>();
		}
	}

	public async Task GiveAllFlags()
	{
		List<(string, List<string>, int, DateTime?)> allPlayers = await GetAllPlayersFlags();

		foreach (var record in allPlayers)
		{
			string steamIdStr = record.Item1;
			List<string> flags = record.Item2;
			int immunity = record.Item3;

			DateTime? ends = record.Item4;

			if (!string.IsNullOrEmpty(steamIdStr) && SteamID.TryParse(steamIdStr, out var steamId) && steamId != null)
			{
				if (!_adminCache.ContainsKey(steamId))
				{
					_adminCache.TryAdd(steamId, ends);
					//_adminCacheTimestamps.Add(steamId, ends);
				}

				Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
				// Often need to call 2 times
				Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
			}
		}
	}

	public async Task DeleteAdminBySteamId(string playerSteamId, bool globalDelete = false)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;

		//_adminCache.TryRemove(playerSteamId, out _);

		await using MySqlConnection connection = await _database.GetConnectionAsync();

		string sql = "";

		if (globalDelete)
		{
			sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID";
		}
		else
		{
			sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID AND FIND_IN_SET(@ServerId, server_id)";
		}

		await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId, ServerId = CSSPanel.ServerId });
	}

	public async Task AddAdminBySteamId(string playerSteamId, string playerName, string flags, int immunity = 0, int time = 0, bool globalAdmin = false)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;

		flags = flags.Replace(" ", "");

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime? futureTime;
		if (time != 0)
			futureTime = now.ToLocalTime().AddMinutes(time);
		else
			futureTime = null;

		await using MySqlConnection connection = await _database.GetConnectionAsync();

		var sql = "INSERT INTO `sa_admins` (`player_steamid`, `player_name`, `flags`, `immunity`, `ends`, `created`, `server_id`) " +
			"VALUES (@playerSteamid, @playerName, @flags, @immunity, @ends, @created, @serverid)";

		int? serverId = globalAdmin ? null : CSSPanel.ServerId;

		await connection.ExecuteAsync(sql, new
		{
			playerSteamId,
			playerName,
			flags,
			immunity,
			ends = futureTime,
			created = now,
			serverid = serverId
		});
	}

	public async Task DeleteOldAdmins()
	{
		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string sql = "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now.ToLocalTime() });
		}
		catch (Exception)
		{
			if (CSSPanel._logger != null)
				CSSPanel._logger.LogCritical("Unable to remove expired admins");
		}
	}
}