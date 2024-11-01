# CSS Panel - CS2 Plugin

NOTE: this plugin is deprecated and should not be used as its not updated and sync with the latest SimpleAdmin versions.
instead, you should use [@Cruze03](https://github.com/Cruze03/CS2-SimpleAdmin) version as it gets updated more frequently and supports the panel.




The plugin is forked and based on [daffyyyy/CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin), included with some additional features and commands for the CSS-Panel web app.
Thanks a lot to [daffyyyy](https://github.com/daffyyyy) for the original plugin, we just added some features and commands to make it work with the CSS-Panel.

### Features

- Everything from [daffyyyy/CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) plugin
- Added new "RCON" db field for servers to store the RCON password and let the panel use it to send RCON commands
- Added new "sm_query" command to query the server for player list and server information through the panel
- Chat logger (based on [oqyh/cs2-Chat-Logger](https://github.com/oqyh/cs2-Chat-Logger/tree/main)) to log chat messages to the database

### Commands

```js
- css_addadmin <steamid> <name> <flags/groups> <immunity> [time in minutes] - Add admin by steamid // @css/root
- css_deladmin <steamid> - Delete admin by steamid // @css/root
- css_reladmin - Reload sql admins // @css/root
- css_hide - Hide admin on scoreboard and commands action // @css/kick
- css_admin - Display all admin commands // @css/generic
- css_who <#userid or name>  - Display informations about player // @css/generic
- css_players - Display player list // @css/generic
- css_ban <#userid or name> [time in minutes/0 perm] [reason] - Ban player // @css/ban
- css_addban <steamid> [time in minutes/0 perm] [reason] - Ban player via steamid64 // @css/ban
- css_banip <ip> [time in minutes/0 perm] [reason] - Ban player via IP address // @css/ban
- css_unban <steamid or name or ip> - Unban player // @css/unban
- css_kick <#userid or name> [reason] - Kick player / @css/kick
- css_gag <#userid or name> [time in minutes/0 perm] [reason] - Gag player // @css/chat
- css_addgag <steamid> [time in minutes/0 perm] [reason] - Gag player via steamid64 // @css/chat
- css_ungag <steamid or name> - Ungag player // @css/chat
- css_mute <#userid or name> [time in minutes/0 perm] [reason] - Mute player // @css/chat
- css_addmute <steamid> [time in minutes/0 perm] [reason] - Mute player via steamid64 // @css/chat
- css_unmute <steamid or name> - Unmute player // @css/chat
- css_silence <#userid or name> [time in minutes/0 perm] [reason] - Silence player // @css/chat
- css_addsilence <steamid> [time in minutes/0 perm] [reason] - Silence player via steamid64 // @css/chat
- css_unsilence <steamid or name> - Unsilence player // @css/chat
- css_give <#userid or name> <weapon> - Give weapon to player // @css/cheats
- css_strip <#userid or name> - Takes all of the player weapons // @css/slay
- css_hp <#userid or name> [health] - Set player health // @css/slay
- css_speed <#userid or name> [speed] - Set player speed // @css/slay
- css_god <#userid or name> - Toggle godmode for player // @css/cheats
- css_slay <#userid or name> - Kill player // @css/slay
- css_slap <#userid or name> [damage] - Slap player // @css/slay
- css_team <#userid or name> [<ct/tt/spec/swap>] [-k] - Change player team (swap - swap player team, -k - kill player) // @css/kick
- css_vote <"Question?"> ["Answer1"] ["Answer2"] ... - Create vote // @css/generic
- css_map <mapname> - Change map // @css/changemap
- css_wsmap <name or id> - Change workshop map // @css/changemap
- css_asay <message> - Say message to all admins // @css/chat
- css_say <message> - Say message as admin in chat // @css/chat
- css_psay <#userid or name> <message> - Sends private message to player // @css/chat
- css_csay <message> - Say message as admin in center // @css/chat
- css_hsay <message> - Say message as admin in hud // @css/chat
- css_noclip <#userid or name> - Toggle noclip for player // @css/cheats
- css_freeze <#userid or name> [duration] - Freeze player // @css/slay
- css_unfreeze <#userid or name> - Unfreeze player // @css/slay
- css_rename <#userid or name> <new name> - Rename player // @css/kick
- css_respawn <#userid or name> - Respawn player // @css/cheats
- css_cvar <cvar> <value> - Change cvar value // @css/cvar
- css_rcon <command> - Run command as server // @css/rcon
- css_give <#userid or name> <WeaponName> - Gives a weapon to a Player // @css/give

- team_chat @Message - Say message to all admins // @css/chat
```

### Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) **tested on v178**
- MySQL **tested on MySQL (MariaDB) Server version: 10.11.4-MariaDB-1~deb12u1 Debian 12**
- [CSS-Panel](https://github.com/CSSPanel/Panel)

### Configuration

After first launch, u need to configure plugin in addons/counterstrikesharp/configs/plugins/CSS-Panel/CSS-Panel.json
