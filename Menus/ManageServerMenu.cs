using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CSSPanel.Menus
{
	public static class ManageServerMenu
	{
		public static void OpenMenu(CCSPlayerController admin)
		{
			if (admin == null || admin.IsValid == false)
				return;

			if (AdminManager.PlayerHasPermissions(admin, "@css/generic") == false)
			{
				// TODO: Localize
				admin.PrintToChat("[Simple Admin] You do not have permissions to use this command.");
				return;
			}

			BaseMenu menu = AdminMenu.CreateMenu("Manage Server");
			List<ChatMenuOptionData> options = new();

			// permissions
			bool hasMap = AdminManager.PlayerHasPermissions(admin, "@css/changemap");

			// TODO: Localize options
			// options added in order

			if (hasMap)
			{
				options.Add(new ChatMenuOptionData("Change Map", () => ChangeMapMenu(admin)));
			}

			options.Add(new ChatMenuOptionData("Restart Game", () => CSSPanel.RestartGame(admin)));

			List<CustomServerCommandData> customCommands = CSSPanel.Instance.Config.CustomServerCommands;
			foreach (CustomServerCommandData customCommand in customCommands)
			{
				if (string.IsNullOrEmpty(customCommand.DisplayName) || string.IsNullOrEmpty(customCommand.Command))
					continue;

				bool hasRights = AdminManager.PlayerHasPermissions(admin, customCommand.Flag);
				if (!hasRights)
					continue;

				options.Add(new ChatMenuOptionData(customCommand.DisplayName, () => Server.ExecuteCommand(customCommand.Command)));
			}

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		public static void ChangeMapMenu(CCSPlayerController admin)
		{
			BaseMenu menu = AdminMenu.CreateMenu($"Change Map");
			List<ChatMenuOptionData> options = new();

			List<string> maps = CSSPanel.Instance.Config.DefaultMaps;
			foreach (string map in maps)
			{
				options.Add(new ChatMenuOptionData(map, () => ExecuteChangeMap(admin, map, false)));
			}

			List<string> wsMaps = CSSPanel.Instance.Config.WorkshopMaps;
			foreach (string map in wsMaps)
			{
				options.Add(new ChatMenuOptionData($"{map} (WS)", () => ExecuteChangeMap(admin, map, true)));
			}

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void ExecuteChangeMap(CCSPlayerController admin, string mapName, bool workshop)
		{
			if (workshop)
				CSSPanel.Instance.ChangeWorkshopMap(admin, mapName);
			else
				CSSPanel.Instance.ChangeMap(admin, mapName);
		}
	}
}