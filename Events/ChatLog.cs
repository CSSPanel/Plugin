using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using Microsoft.Extensions.Logging;
using static Dapper.SqlMapper;

namespace CSSPanel;

public partial class CSSPanel
{
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
}