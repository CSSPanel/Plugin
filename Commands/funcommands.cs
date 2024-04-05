﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using System.Text;

namespace CSSPanel
{
	public partial class CSSPanel
	{
		[ConsoleCommand("css_noclip", "Noclip a player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/cheats")]
		public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && player.PawnIsAlive && !player.IsHLTV).ToList();

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					NoClip(caller, player, callerName);
				}
			});
		}

		public void NoClip(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			player!.Pawn.Value!.ToggleNoclip();

			Helper.LogCommand(caller, $"css_noclip {player.PlayerName}");

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_noclip_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_freeze", "Freeze a player.")]
		[CommandHelper(1, "<#userid or name> [duration]")]
		[RequiresPermissions("@css/slay")]
		public void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			int.TryParse(command.GetArg(2), out int time);

			TargetResult? targets = GetTarget(command);
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Freeze(caller, player, time, callerName);
				}
			});
		}

		public void Freeze(CCSPlayerController? caller, CCSPlayerController player, int time, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.Pawn.Value!.Freeze();

			Helper.LogCommand(caller, $"css_freeze {player.PlayerName}");

			if (time > 0)
				AddTimer(time, () => player.Pawn.Value!.Unfreeze(), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_freeze_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_unfreeze", "Unfreeze a player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/slay")]
		public void OnUnfreezeCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				Unfreeze(caller, player, callerName, command);
			});
		}

		public void Unfreeze(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player!.Pawn.Value!.Unfreeze();

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_unfreeze_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}
	}
}