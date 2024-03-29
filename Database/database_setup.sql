CREATE TABLE IF NOT EXISTS `sa_bans` (
                                `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                                `player_steamid` VARCHAR(64),
                                `player_name` VARCHAR(128),
                                `player_ip` VARCHAR(128),
                                `admin_steamid` VARCHAR(64) NOT NULL,
                                `admin_name` VARCHAR(128) NOT NULL,
                                `reason` VARCHAR(255) NOT NULL,
								`unban_reason` TEXT NULL DEFAULT NULL,
						 		`comment` TEXT NULL DEFAULT NULL,
                                `duration` INT NOT NULL,
                                `ends` TIMESTAMP NOT NULL,
                                `created` TIMESTAMP NOT NULL,
								`server_id` INT NULL,
                                `status` ENUM('ACTIVE', 'UNBANNED', 'EXPIRED', '') NOT NULL DEFAULT 'ACTIVE'
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_mutes` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `player_steamid` varchar(64) NOT NULL,
						 `player_name` varchar(128) NULL,
						 `admin_steamid` varchar(64) NOT NULL,
						 `admin_name` varchar(128) NOT NULL,
						 `reason` varchar(255) NOT NULL,
						 `unmute_reason` TEXT NULL DEFAULT NULL,
						 `comment` TEXT NULL DEFAULT NULL,
						 `duration` int(11) NOT NULL,
						 `ends` timestamp NOT NULL,
						 `created` timestamp NOT NULL,
						 `type` enum('GAG','MUTE','SILENCE','') NOT NULL DEFAULT 'GAG',
						 `server_id` INT NULL,
						 `status` enum('ACTIVE','UNMUTED','EXPIRED','') NOT NULL DEFAULT 'ACTIVE',
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_admins` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `player_steamid` varchar(64) NOT NULL,
						 `player_name` varchar(128) NOT NULL,
						 `flags` TEXT NOT NULL,
						 `immunity` varchar(64) NOT NULL DEFAULT '0',
						 `server_id` INT NULL,
						 `ends` timestamp NULL,
						 `created` timestamp NOT NULL,
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_admins_groups` (
						 `id` VARCHAR(50) NOT NULL,
						 `name` TEXT NOT NULL,
						 `flags` TEXT NOT NULL,
						 `immunity` varchar(64) NOT NULL DEFAULT '0',
						 `created` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
						 PRIMARY KEY (`id`)
						) ENGINE = InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_servers` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `address` varchar(64) NOT NULL,
						 `hostname` varchar(64) NOT NULL,
						 `rcon` varchar(64) NULL,
						 PRIMARY KEY (`id`),
						 UNIQUE KEY `address` (`address`)
						) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ALTER TABLE `sa_servers` ADD COLUMN IF NOT EXISTS `rcon` VARCHAR(64) NULL;
ALTER TABLE `sa_admins` CHANGE `server_id` `server_id` VARCHAR(50) NULL DEFAULT NULL;

CREATE TABLE IF NOT EXISTS `sa_chatlogs` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `serverId` varchar(64) NOT NULL,
						 `playerSteam64` varchar(64) NOT NULL,
						 `playerName` varchar(64) NOT NULL,
						 `message` TEXT,
						 `team` BOOLEAN NOT NULL,
						 `created` timestamp NOT NULL,
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;