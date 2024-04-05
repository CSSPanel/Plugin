using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CSSPanel
{
	public class Discord
	{
		[JsonPropertyName("DiscordLogWebhook")]
		public string DiscordLogWebhook { get; set; } = "";

		[JsonPropertyName("DiscordPenaltyWebhook")]
		public string DiscordPenaltyWebhook { get; set; } = "";
	}

	public class ChatLog
	{
		[JsonPropertyName("ChatLog_Enable")]
		public bool ChatLog_Enable { get; set; } = true;

		[JsonPropertyName("ChatLog_ExcludeMessageContains")]
		public string ExcludeMessageContains { get; set; } = "!./";

		[JsonPropertyName("ChatLog_ExcludeMessageContainsLessThanXLetters")]
		public int ExcludeMessageContainsLessThanXLetters { get; set; } = 0;

		[JsonPropertyName("ChatLog_ExcludeMessageDuplicate")]
		public bool ExcludeMessageDuplicate { get; set; } = false;
	}

	public class Statistics
	{
		[JsonPropertyName("Statistics_Enable")]
		public bool Statistics_Enable { get; set; } = true;
	}

	public class CustomServerCommandData
	{
		[JsonPropertyName("Flag")]
		public string Flag { get; set; } = "@css/generic";

		[JsonPropertyName("DisplayName")]
		public string DisplayName { get; set; } = "";

		[JsonPropertyName("Command")]
		public string Command { get; set; } = "";

		[JsonPropertyName("ExecuteOnClient")]
		public bool ExecuteOnClient { get; set; } = false;
	}

	public class CSSPanelConfig : BasePluginConfig
	{
		[JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 9;

		[JsonPropertyName("DatabaseHost")]
		public string DatabaseHost { get; set; } = "";

		[JsonPropertyName("DatabasePort")]
		public int DatabasePort { get; set; } = 3306;

		[JsonPropertyName("DatabaseUser")]
		public string DatabaseUser { get; set; } = "";

		[JsonPropertyName("DatabasePassword")]
		public string DatabasePassword { get; set; } = "";

		[JsonPropertyName("DatabaseName")]
		public string DatabaseName { get; set; } = "";

		[JsonPropertyName("UseChatMenu")]
		public bool UseChatMenu { get; set; } = false;

		[JsonPropertyName("KickTime")]
		public int KickTime { get; set; } = 5;

		[JsonPropertyName("DisableDangerousCommands")]
		public bool DisableDangerousCommands { get; set; } = true;

		[JsonPropertyName("BanType")]
		public int BanType { get; set; } = 1;

		[JsonPropertyName("ExpireOldIpBans")]
		public int ExpireOldIpBans { get; set; } = 0;

		[JsonPropertyName("TeamSwitchType")]
		public int TeamSwitchType { get; set; } = 1;

		[JsonPropertyName("Discord")]
		public Discord Discord { get; set; } = new Discord();

		[JsonPropertyName("DefaultMaps")]
		public List<string> DefaultMaps { get; set; } = new List<string>();

		[JsonPropertyName("WorkshopMaps")]
		public List<string> WorkshopMaps { get; set; } = new List<string>();

		[JsonPropertyName("CustomServerCommands")]
		public List<CustomServerCommandData> CustomServerCommands { get; set; } = new List<CustomServerCommandData>();

		[JsonPropertyName("DefaultServerIP")]
		public string DefaultServerIP { get; set; } = "";

		[JsonPropertyName("PanelURL")]
		public string PanelURL { get; set; } = "";

		[JsonPropertyName("ChatLog")]
		public ChatLog ChatLog { get; set; } = new ChatLog();
		
		[JsonPropertyName("Statistics")]
		public Statistics Statistics { get; set; } = new Statistics();
	}
}