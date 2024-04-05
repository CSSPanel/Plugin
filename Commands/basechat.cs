﻿﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace CSSPanel
{
	public partial class CSSPanel
	{
		[ConsoleCommand("css_asay", "Say to all admins.")]
		[CommandHelper(1, "<message>")]
		[RequiresPermissions("@css/chat")]
		public void OnAdminToAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null || !caller.IsValid || command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
			string utf8String = Encoding.UTF8.GetString(utf8BytesString);

			foreach (CCSPlayerController _player in Helper.GetValidPlayers().Where(p => AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				using (new WithTemporaryCulture(_player.GetLanguage()))
				{
					StringBuilder sb = new();
					sb.Append(_localizer!["sa_adminchat_template_admin", caller == null ? "Console" : caller.PlayerName, utf8String]);
					_player.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_say", "Say to all players.")]
		[CommandHelper(1, "<message>")]
		[RequiresPermissions("@css/chat")]
		public void OnAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

			string callerName = caller == null ? "Console" : caller.PlayerName;
			byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
			string utf8String = Encoding.UTF8.GetString(utf8BytesString);

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			foreach (CCSPlayerController _player in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(_player.GetLanguage()))
				{
					StringBuilder sb = new();
					sb.Append(_localizer!["sa_adminsay_prefix", utf8String]);
					_player.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_psay", "Private message a player.")]
		[CommandHelper(2, "<#userid or name> <message>")]
		[RequiresPermissions("@css/chat")]
		public void OnAdminPrivateSayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			//Helper.LogCommand(caller, command);

			int range = command.GetArg(0).Length + command.GetArg(1).Length + 2;
			string message = command.GetCommandString[range..];

			byte[] utf8BytesString = Encoding.UTF8.GetBytes(message);
			string utf8String = Encoding.UTF8.GetString(utf8BytesString);

			playersToTarget.ForEach(player =>
			{
				player.PrintToChat(StringExtensions.ReplaceColorTags($"({callerName}) {utf8String}"));
			});

			command.ReplyToCommand(StringExtensions.ReplaceColorTags($" Private message sent!"));
		}

		[ConsoleCommand("css_csay", "Say to all players (in center).")]
		[CommandHelper(1, "<message>")]
		[RequiresPermissions("@css/chat")]
		public void OnAdminCenterSayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
			string utf8String = Encoding.UTF8.GetString(utf8BytesString);

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			Helper.PrintToCenterAll(StringExtensions.ReplaceColorTags(utf8String));
		}

		[ConsoleCommand("css_hsay", "Say to all players (in hud).")]
		[CommandHelper(1, "<message>")]
		[RequiresPermissions("@css/chat")]
		public void OnAdminHudSayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
			string utf8String = Encoding.UTF8.GetString(utf8BytesString);

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			VirtualFunctions.ClientPrintAll(
				HudDestination.Alert,
				StringExtensions.ReplaceColorTags(utf8String),
				0, 0, 0, 0);
		}
	}
}