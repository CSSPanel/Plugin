﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Dapper;
using Discord.Webhook;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CSSPanel;

[MinimumApiVersion(246)]
public partial class CSSPanel : BasePlugin, IPluginConfig<CSSPanelConfig>
{
	public static CSSPanel Instance { get; private set; } = new();

	public static IStringLocalizer? _localizer;
	public static readonly Dictionary<string, int> VoteAnswers = [];
	private static bool _serverLoaded;
	private static readonly HashSet<int> GodPlayers = [];
	private static readonly HashSet<int> SilentPlayers = [];
	private static readonly ConcurrentBag<string> BannedPlayers = [];
	private static readonly Dictionary<ulong, string> RenamedPlayers = [];
	//private static readonly ConcurrentBag<int> SilentPlayers = [];
	private static bool _tagsDetected;
	public static bool VoteInProgress = false;
	public static int? ServerId = null;
	public static bool UnlockedCommands = CoreConfig.UnlockConCommands;

	public static DiscordWebhookClient? DiscordWebhookClientLog;
	public static DiscordWebhookClient? DiscordWebhookClientPenalty;

	private string _dbConnectionString = string.Empty;
	private static Database.Database? _database;

	internal static ILogger? _logger;

	private static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? _cBasePlayerControllerSetPawnFunc;
	public override string ModuleName => "CSS-Panel" + (Helper.IsDebugBuild ? " (DEBUG)" : " (RELEASE)");
	public override string ModuleDescription => "Simple admin plugin for Counter-Strike 2 :)";
	public override string ModuleAuthor => "daffyy & Dliix66 & ShiNxz (CssPanel)";
	public override string ModuleVersion => "1.5.3";

	private static readonly HttpClient httpClient = new HttpClient();
	static private Dictionary<int, bool> BteamChat = new Dictionary<int, bool>();

	static string firstMessage = "";
	static string secondMessage = "";

	public CSSPanelConfig Config { get; set; } = new();

	public override void Load(bool hotReload)
	{
		Instance = this;

		RegisterEvents();

		if (hotReload)
		{
			_serverLoaded = false;
			OnMapStart(string.Empty);
		}

		_cBasePlayerControllerSetPawnFunc = new MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>(GameData.GetSignature("CBasePlayerController_SetPawn"));
	}

	public override void Unload(bool hotReload)
	{
		if (hotReload) return;

		RemoveListener(OnMapStart);
		RemoveCommandListener("say", OnCommandSay, HookMode.Post);
		RemoveCommandListener("say_team", OnCommandTeamSay, HookMode.Post);
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		AddTimer(3.0f, () => ReloadAdmins(null));
	}

	public void OnConfigParsed(CSSPanelConfig config)
	{
		Console.WriteLine("[CSS-Panel] Config parsed!");

		if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
		{
			throw new Exception("[CSS-Panel] You need to setup Database credentials in config!");
		}

		Instance = this;
		_logger = Logger;

		MySqlConnectionStringBuilder builder = new()
		{
			Server = config.DatabaseHost,
			Database = config.DatabaseName,
			UserID = config.DatabaseUser,
			Password = config.DatabasePassword,
			Port = (uint)config.DatabasePort,
			Pooling = true,
			MinimumPoolSize = 0,
			MaximumPoolSize = 640,
		};

		_dbConnectionString = builder.ConnectionString;
		_database = new Database.Database(_dbConnectionString);

		if (!_database.CheckDatabaseConnection())
		{
			Logger.LogError("Unable connect to database!");
			Unload(false);
			return;
		}

		Task.Run(async () =>
		{
			try
			{
				using MySqlConnection connection = await _database.GetConnectionAsync();
				using MySqlTransaction transaction = await connection.BeginTransactionAsync();

				try
				{
					string sqlFilePath = ModuleDirectory + "/Database/database_setup.sql";
					string sql = await File.ReadAllTextAsync(sqlFilePath);

					await connection.QueryAsync(sql, transaction: transaction);
					await transaction.CommitAsync();

					Console.WriteLine("[CSS-Panel] Connected to database!");
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Unable to connect to the database: {ex.Message}");
				throw;
			}
		});

		Config = config;
		Helper.UpdateConfig(config);

		if (!Directory.Exists(ModuleDirectory + "/data"))
		{
			Directory.CreateDirectory(ModuleDirectory + "/data");
		}

		_localizer = Localizer;

		if (!string.IsNullOrEmpty(Config.Discord.DiscordLogWebhook))
			DiscordWebhookClientLog = new DiscordWebhookClient(Config.Discord.DiscordLogWebhook);
		if (!string.IsNullOrEmpty(Config.Discord.DiscordPenaltyWebhook))
			DiscordWebhookClientPenalty = new DiscordWebhookClient(Config.Discord.DiscordPenaltyWebhook);
	}

	private static TargetResult? GetTarget(CommandInfo command)
	{
		var matches = command.GetArgTargetResult(1);

		if (!matches.Any())
		{
			command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
			return null;
		}

		if (command.GetArg(1).StartsWith('@'))
			return matches;

		if (matches.Count() == 1)
			return matches;

		command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
		return null;
	}

	private static void RemoveFromConcurrentBag(ConcurrentBag<int> bag, int playerSlot)
	{
		List<int> tempList = [];
		while (!bag.IsEmpty)
		{
			if (bag.TryTake(out var item) && item != playerSlot)
			{
				tempList.Add(item);
			}
		}

		foreach (var item in tempList)
		{
			bag.Add(item);
		}
	}
}