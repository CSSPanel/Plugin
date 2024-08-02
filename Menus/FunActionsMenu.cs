using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace CSSPanel.Menus
{
	public static class FunActionsMenu
	{
		private static Dictionary<int, CsItem>? _weaponsCache;

		private static Dictionary<int, CsItem> GetWeaponsCache
		{
			get
			{
				if (_weaponsCache != null) return _weaponsCache;
				
				var weaponsArray = Enum.GetValues(typeof(CsItem));

				// avoid duplicates in the menu
				_weaponsCache = new Dictionary<int, CsItem>();
				foreach (CsItem item in weaponsArray)
				{
					if (item == CsItem.Tablet)
						continue;

					_weaponsCache[(int)item] = item;
				}

				return _weaponsCache;
			}
		}

		public static void OpenMenu(CCSPlayerController admin)
		{
			if (admin.IsValid == false)
				return;

			var localizer = CSSPanel._localizer;
			if (AdminManager.PlayerHasPermissions(admin, "@css/generic") == false)
			{
				admin.PrintToChat(localizer?["sa_prefix"] ??
				                  "[Admin] " + 
				                  (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
				);
				return;
			}

			var menu = AdminMenu.CreateMenu(localizer?["sa_menu_fun_commands"] ?? "Fun Commands");
			List<ChatMenuOptionData> options = [];

			// permissions
			var hasCheats = AdminManager.PlayerHasPermissions(admin, "@css/cheats");
			var hasSlay = AdminManager.PlayerHasPermissions(admin, "@css/slay");

			// options added in order

			if (hasCheats)
			{
				options.Add(new ChatMenuOptionData(localizer?["sa_godmode"] ?? "God Mode", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_godmode"] ?? "God Mode", GodMode)));
				options.Add(new ChatMenuOptionData(localizer?["sa_noclip"] ?? "No Clip", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_noclip"] ?? "No Clip", NoClip)));
				options.Add(new ChatMenuOptionData(localizer?["sa_respawn"] ?? "Respawn", () => PlayersMenu.OpenDeadMenu(admin, localizer?["sa_respawn"] ?? "Respawn", Respawn)));
				options.Add(new ChatMenuOptionData(localizer?["sa_give_weapon"] ?? "Give Weapon", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_give_weapon"] ?? "Give Weapon", GiveWeaponMenu)));
			}

			if (hasSlay)
			{
				options.Add(new ChatMenuOptionData(localizer?["sa_strip_weapons"] ?? "Strip Weapons", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_strip_weapons"] ?? "Strip Weapons", StripWeapons)));
				options.Add(new ChatMenuOptionData(localizer?["sa_freeze"] ?? "Freeze", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_freeze"] ?? "Freeze", Freeze)));
				options.Add(new ChatMenuOptionData(localizer?["sa_set_hp"] ?? "Set Hp", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_hp"] ?? "Set Hp", SetHpMenu)));
				options.Add(new ChatMenuOptionData(localizer?["sa_set_speed"] ?? "Set Speed", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_speed"] ?? "Set Speed", SetSpeedMenu)));
				options.Add(new ChatMenuOptionData(localizer?["sa_set_gravity"] ?? "Set Gravity", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_gravity"] ?? "Set Gravity", SetGravityMenu)));
				options.Add(new ChatMenuOptionData(localizer?["sa_set_money"] ?? "Set Money", () => PlayersMenu.OpenMenu(admin, localizer?["sa_set_money"] ?? "Set Money", SetMoneyMenu)));
			}

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void GodMode(CCSPlayerController admin, CCSPlayerController? player)
		{
			CSSPanel.Instance.God(admin, player);
		}

		private static void NoClip(CCSPlayerController admin, CCSPlayerController? player)
		{
			CSSPanel.Instance.NoClip(admin, player);
		}

		private static void Respawn(CCSPlayerController admin, CCSPlayerController? player)
		{
			CSSPanel.Instance.Respawn(admin, player);
		}

		private static void GiveWeaponMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_give_weapon"] ?? "Give Weapon"}: {player.PlayerName}");

			foreach (var weapon in GetWeaponsCache)
			{
				menu.AddMenuOption(weapon.Value.ToString(), (_, _) => { GiveWeapon(admin, player, weapon.Value); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void GiveWeapon(CCSPlayerController admin, CCSPlayerController player, CsItem weaponValue)
		{
			CSSPanel.Instance.GiveWeapon(admin, player, weaponValue);
		}

		private static void StripWeapons(CCSPlayerController admin, CCSPlayerController? player)
		{
			CSSPanel.Instance.StripWeapons(admin, player);
		}

		private static void Freeze(CCSPlayerController admin, CCSPlayerController? player)
		{
			if (!(player?.PlayerPawn.Value?.IsValid ?? false))
				return;

			if (player.PlayerPawn.Value.MoveType != MoveType_t.MOVETYPE_OBSOLETE)
				CSSPanel.Instance.Freeze(admin, player, -1);
			else
				CSSPanel.Instance.Unfreeze(admin, player);
		}

		private static void SetHpMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			var hpArray = new[]
			{
				new Tuple<string, int>("1", 1),
				new Tuple<string, int>("10", 10),
				new Tuple<string, int>("25", 25),
				new Tuple<string, int>("50", 50),
				new Tuple<string, int>("100", 100),
				new Tuple<string, int>("200", 200),
				new Tuple<string, int>("500", 500),
				new Tuple<string, int>("999", 999)
			};

			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_set_hp"] ?? "Set Hp"}: {player?.PlayerName}");

			foreach (var (optionName, value) in hpArray)
			{
				menu.AddMenuOption(optionName, (_, _) => { SetHp(admin, player, value); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void SetHp(CCSPlayerController admin, CCSPlayerController? player, int hp)
		{
			CSSPanel.Instance.SetHp(admin, player, hp);
		}

		private static void SetSpeedMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			var speedArray = new[]
			{
				new Tuple<string, float>("0.1", .1f),
				new Tuple<string, float>("0.25", .25f),
				new Tuple<string, float>("0.5", .5f),
				new Tuple<string, float>("0.75", .75f),
				new Tuple<string, float>("1", 1),
				new Tuple<string, float>("2", 2),
				new Tuple<string, float>("3", 3),
				new Tuple<string, float>("4", 4)
			};

			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_set_speed"] ?? "Set  Speed"}: {player?.PlayerName}");

			foreach (var (optionName, value) in speedArray)
			{
				menu.AddMenuOption(optionName, (_, _) => { SetSpeed(admin, player, value); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void SetSpeed(CCSPlayerController admin, CCSPlayerController? player, float speed)
		{
			CSSPanel.Instance.SetSpeed(admin, player, speed);
		}

		private static void SetGravityMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			var gravityArray = new[]
			{
				new Tuple<string, float>("0.1", .1f),
				new Tuple<string, float>("0.25", .25f),
				new Tuple<string, float>("0.5", .5f),
				new Tuple<string, float>("0.75", .75f),
				new Tuple<string, float>("1", 1),
				new Tuple<string, float>("2", 2)
			};

			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_set_gravity"] ?? "Set Gravity"}: {player?.PlayerName}");

			foreach (var (optionName, value) in gravityArray)
			{
				menu.AddMenuOption(optionName, (_, _) => { SetGravity(admin, player, value); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void SetGravity(CCSPlayerController admin, CCSPlayerController? player, float gravity)
		{
			CSSPanel.Instance.SetGravity(admin, player, gravity);
		}

		private static void SetMoneyMenu(CCSPlayerController admin, CCSPlayerController? player)
		{
			var moneyArray = new[]
			{
				new Tuple<string, int>("$0", 0),
				new Tuple<string, int>("$1000", 1000),
				new Tuple<string, int>("$2500", 2500),
				new Tuple<string, int>("$5000", 5000),
				new Tuple<string, int>("$10000", 10000),
				new Tuple<string, int>("$16000", 16000)
			};

			var menu = AdminMenu.CreateMenu($"{CSSPanel._localizer?["sa_set_money"] ?? "Set Money"}: {player?.PlayerName}");

			foreach (var (optionName, value) in moneyArray)
			{
				menu.AddMenuOption(optionName, (_, _) => { SetMoney(admin, player, value); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void SetMoney(CCSPlayerController admin, CCSPlayerController? player, int money)
		{
			CSSPanel.Instance.SetMoney(admin, player, money);
		}
	}
}
