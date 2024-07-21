﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace CSSPanel
{
	public partial class CSSPanel
	{
		[ConsoleCommand("css_vote")]
		[RequiresPermissions("@css/generic")]
		[CommandHelper(minArgs: 2, usage: "<question> [... options ...]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (command.ArgCount < 2)
				return;

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			VoteAnswers.Clear();

			var question = command.GetArg(1);
			var answersCount = command.ArgCount;

			if (caller == null || !SilentPlayers.Contains(caller.Slot))
			{
				for (var i = 2; i <= answersCount - 1; i++)
				{
					VoteAnswers.Add(command.GetArg(i), 0);
				}

				foreach (var player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						BaseMenu voteMenu = Config.UseChatMenu
							? new ChatMenu(_localizer!["sa_admin_vote_menu_title", question])
							: new CenterHtmlMenu(_localizer!["sa_admin_vote_menu_title", question], Instance);
						//ChatMenu voteMenu = new(_localizer!["sa_admin_vote_menu_title", question]);

						for (var i = 2; i <= answersCount - 1; i++)
						{
							voteMenu.AddMenuOption(command.GetArg(i), Helper.HandleVotes);
						}

						voteMenu.PostSelectAction = PostSelectAction.Close;

						Helper.PrintToCenterAll(_localizer["sa_admin_vote_message", caller == null ? "Server" : caller.PlayerName, question]);

						if (_localizer != null)
							player.SendLocalizedMessage(_localizer,
												"sa_admin_vote_message",
												caller == null ? "Server" : caller.PlayerName,
												question);

						voteMenu.Open(player);

						//MenuManager.OpenChatMenu(player, voteMenu);
					}
				}

				VoteInProgress = true;
			}

			if (VoteInProgress)
			{
				AddTimer(30, () =>
				{
					foreach (var player in Helper.GetValidPlayers())
					{
						if (_localizer != null)
							player.SendLocalizedMessage(_localizer,
												"sa_admin_vote_message_results",
												question);
					}

					foreach (var (key, value) in VoteAnswers)
					{
						foreach (var player in Helper.GetValidPlayers())
						{
							if (_localizer != null)
								player.SendLocalizedMessage(_localizer,
													"sa_admin_vote_message_results_answer",
													key,
													value);
						}
					}
					VoteAnswers.Clear();
					VoteInProgress = false;
				}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
		}
	}
}