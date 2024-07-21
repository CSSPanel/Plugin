using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace CSSPanel.Menus
{
	public static class ManageAdminsMenu
	{
		public static void OpenMenu(CCSPlayerController admin)
		{
			if (admin.IsValid == false)
				return;

			var localizer = CSSPanel._localizer;
			if (AdminManager.PlayerHasPermissions(admin, "@css/root") == false)
			{
				admin.PrintToChat(localizer?["sa_prefix"] ??
				                  "[SimpleAdmin] " + 
				                  (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
				);
				return;
			}

			var menu = AdminMenu.CreateMenu(localizer?["sa_menu_admins_manage"] ?? "Admins Manage");
			List<ChatMenuOptionData> options =
			[
				new ChatMenuOptionData(localizer?["sa_admin_add"] ?? "Add Admin",
					() => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_admin_add"] ?? "Add Admin", AddAdminMenu)),
				new ChatMenuOptionData(localizer?["sa_admin_remove"] ?? "Remove Admin",
					() => PlayersMenu.OpenAdminPlayersMenu(admin, localizer?["sa_admin_remove"] ?? "Remove Admin", RemoveAdmin,
						player => player != admin && admin.CanTarget(player))),
				new ChatMenuOptionData(localizer?["sa_admin_reload"] ?? "Reload Admins", () => ReloadAdmins(admin))
			];

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void AddAdminMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_admin_add"] ?? "Add Admin"}: {player.PlayerName}");

			foreach (var adminFlag in CSSPanel.Instance.Config.MenuConfigs.AdminFlags)
			{
				var disabled = AdminManager.PlayerHasPermissions(player, adminFlag.Flag);
				menu.AddMenuOption(adminFlag.Name, (_, _) => { AddAdmin(admin, player, adminFlag.Flag); }, disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void AddAdmin(CCSPlayerController admin, CCSPlayerController player, string flag)
		{
			// TODO: Change default immunity?
			CSSPanel.AddAdmin(admin, player.SteamID.ToString(), player.PlayerName, flag, 10);
		}

		private static void RemoveAdmin(CCSPlayerController admin, CCSPlayerController player)
		{
			CSSPanel.Instance.RemoveAdmin(admin, player.SteamID.ToString());
		}

		private static void ReloadAdmins(CCSPlayerController admin)
		{
			CSSPanel.Instance.ReloadAdmins(admin);
		}
	}
}