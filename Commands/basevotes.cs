﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text;

namespace CSSPanel
{
	public partial class CSSPanel
	{
		[ConsoleCommand("css_vote")]
		[RequiresPermissions("@css/generic")]
		[CommandHelper(minArgs: 2, usage: "<question> [... options ...]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.GetArg(1) == null || command.GetArg(1).Length < 0 || command.ArgCount < 2)
				return;

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			voteAnswers.Clear();

			string question = command.GetArg(1);
			int answersCount = command.ArgCount;

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				for (int i = 2; i <= answersCount - 1; i++)
				{
					voteAnswers.Add(command.GetArg(i), 0);
				}

				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						ChatMenu voteMenu = new(_localizer!["sa_admin_vote_menu_title", question]);

						for (int i = 2; i <= answersCount - 1; i++)
						{
							voteMenu.AddMenuOption(command.GetArg(i), Helper.HandleVotes);
						}

						voteMenu.PostSelectAction = PostSelectAction.Close;

						Helper.PrintToCenterAll(_localizer!["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
						_player.PrintToChat(sb.ToString());

						MenuManager.OpenChatMenu(_player, voteMenu);
					}
				}

				voteInProgress = true;
			}

			if (voteInProgress)
			{
				AddTimer(30, () =>
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_vote_message_results", question]);
							_player.PrintToChat(sb.ToString());
						}
					}

					foreach (KeyValuePair<string, int> kvp in voteAnswers)
					{
						foreach (CCSPlayerController _player in Helper.GetValidPlayers())
						{
							using (new WithTemporaryCulture(_player.GetLanguage()))
							{
								StringBuilder sb = new(_localizer!["sa_prefix"]);
								sb.Append(_localizer["sa_admin_vote_message_results_answer", kvp.Key, kvp.Value]);
								_player.PrintToChat(sb.ToString());
							}
						}
					}
					voteAnswers.Clear();
					voteInProgress = false;
				}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
		}
	}
}