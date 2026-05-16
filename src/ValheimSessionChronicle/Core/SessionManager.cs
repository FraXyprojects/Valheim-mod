using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Discord;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Storage;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Core
{
    public sealed class SessionManager
    {
        private static readonly TimeSpan RecentLocalDamageWindow = TimeSpan.FromSeconds(30);

        private readonly ChronicleConfig _config;
        private readonly SessionStorage _storage;
        private readonly DiscordWebhookClient _discordWebhookClient;
        private readonly EventManager _events = new EventManager();
        private readonly HashSet<string> _seenPlayerSpawns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, DateTime> _recentLocalDamageTargets = new Dictionary<int, DateTime>();
        private readonly Dictionary<string, DateTime> _lastCountByKey = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        private SessionData _current;
        private bool _ending;

        public SessionManager(ChronicleConfig config, SessionStorage storage, DiscordWebhookClient discordWebhookClient)
        {
            _config = config;
            _storage = storage;
            _discordWebhookClient = discordWebhookClient;
        }

        public bool IsActive => _current != null;

        public SessionData Current => _current;

        public void StartSession(string explicitReason = null)
        {
            if (_current != null)
            {
                return;
            }

            string localPlayer = ValheimNames.GetLocalPlayerName();

            _current = new SessionData
            {
                ModVersion = ValheimSessionChroniclePlugin.PluginVersion,
                StartTimeUtc = DateTime.UtcNow,
                LocalPlayerName = localPlayer,
                ServerName = ValheimNames.GetServerName(),
                WorldName = ValheimNames.GetWorldName(),
                IsMultiplayerClient = ValheimState.IsMultiplayerClient()
            };

            _events.Begin(_current);
            AddKnownPlayer(localPlayer);

                _events.Add(
                    EventTypes.SessionStarted,
                    EventCategories.Session,
                $"Session začala na serveru {_current.ServerName}.",
                actor: localPlayer,
                importance: 3);

            _events.Add(
                EventTypes.PlayerJoined,
                EventCategories.Player,
                $"{localPlayer} se připojil k serveru.",
                actor: localPlayer,
                importance: 3);

            ChronicleLogger.Info($"Session started. Server='{_current.ServerName}', player='{localPlayer}'. {explicitReason}");
        }

        public void EndSession(string reason, DisconnectReason disconnectReason)
        {
            if (_current == null || _ending)
            {
                return;
            }

            _ending = true;
            try
            {
                _current.EndTimeUtc = DateTime.UtcNow;
                _current.DisconnectReason = disconnectReason.ToString();

                _events.Add(
                    EventTypes.SessionEnded,
                    EventCategories.Session,
                    $"Hráč byl odpojen. Důvod: {reason}",
                    actor: _current.LocalPlayerName,
                    importance: 3,
                    metadata: new Dictionary<string, string> { ["DisconnectReason"] = disconnectReason.ToString() });

                StorageResult result = _storage.Save(_current, _config);

                if (_config.EnableDiscordWebhook.Value && !string.IsNullOrWhiteSpace(_config.DiscordWebhookURL.Value))
                {
                    _discordWebhookClient.SendPlainTextAsync(_config.DiscordWebhookURL.Value, result.TxtReport ?? string.Empty);
                }

                ChronicleLogger.Info($"Session ended. TXT='{result.TxtPath}', JSON='{result.JsonPath}'.");
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Failed while saving the session report.");
            }
            finally
            {
                _events.End();
                _current = null;
                _seenPlayerSpawns.Clear();
                _recentLocalDamageTargets.Clear();
                _lastCountByKey.Clear();
                _ending = false;
            }
        }

        public void AddKnownPlayer(string playerName)
        {
            if (_current == null || string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            if (!_current.Players.Contains(playerName, StringComparer.OrdinalIgnoreCase))
            {
                _current.Players.Add(playerName);
                EnsurePlayerStats(playerName);
            }
        }

        public void RecordVisiblePlayer(Player player)
        {
            if (_current == null || player == null)
            {
                return;
            }

            // Remote players are only visible when Valheim has replicated them to this client.
            AddKnownPlayer(ValheimNames.GetPlayerName(player));
        }

        public void RecordPlayerDeath(Player player)
        {
            if (_current == null || player == null)
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            AddKnownPlayer(playerName);
            if (!ShouldCount("death:" + playerName, TimeSpan.FromSeconds(5)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            stats.Deaths++;

            _events.Add(
                EventTypes.PlayerDeath,
                EventCategories.Player,
                $"{playerName} zemřel.",
                actor: playerName,
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: 3,
                duplicateKey: "death:" + playerName,
                duplicateCooldownSeconds: 5);
        }

        public void RecordPlayerRespawn(Player player)
        {
            if (_current == null || player == null)
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            AddKnownPlayer(playerName);

            if (!_seenPlayerSpawns.Add(playerName))
            {
                PlayerStats stats = EnsurePlayerStats(playerName);
                stats.Respawns++;

                _events.Add(
                    EventTypes.PlayerRespawn,
                    EventCategories.Player,
                    $"{playerName} se znovu objevil ve světě.",
                    actor: playerName,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: 2,
                    duplicateKey: "respawn:" + playerName,
                    duplicateCooldownSeconds: 10);
            }
        }

        public void RecordBiomeEntered(string playerName, string biomeName, Vector3 position)
        {
            if (_current == null || string.IsNullOrWhiteSpace(biomeName))
            {
                return;
            }

            playerName = string.IsNullOrWhiteSpace(playerName) ? _current.LocalPlayerName : playerName;
            AddKnownPlayer(playerName);

            PlayerStats stats = EnsurePlayerStats(playerName);
            bool firstForPlayer = AddUnique(stats.BiomesVisited, biomeName);
            bool firstForSession = AddUnique(_current.Environment.BiomesVisited, biomeName);

            if (firstForSession)
            {
                _current.Environment.FirstBiomeVisitUtc[biomeName] = DateTime.UtcNow;
            }

            if (firstForPlayer)
            {
                string prefix = firstForSession ? "První návštěva biomu" : $"{playerName} vstoupil do biomu";
                _events.Add(
                    firstForSession ? EventTypes.Discovery : EventTypes.BiomeEntered,
                    firstForSession ? EventCategories.Progression : EventCategories.Environment,
                    $"{prefix} {biomeName}.",
                    actor: playerName,
                    biome: biomeName,
                    position: ValheimNames.FormatPosition(position),
                    importance: firstForSession ? 3 : 2,
                    duplicateKey: "biome:" + playerName + ":" + biomeName,
                    duplicateCooldownSeconds: 30);
            }
        }

        public void RecordWeatherChanged(string weatherName)
        {
            if (_current == null || !_config.TrackEnvironment.Value || string.IsNullOrWhiteSpace(weatherName))
            {
                return;
            }

            _current.Environment.WeatherChanges++;
            AddUnique(_current.Environment.WeatherSeen, weatherName);

            _events.Add(
                EventTypes.WeatherChanged,
                EventCategories.Environment,
                $"Počasí se změnilo na {weatherName}.",
                target: weatherName,
                importance: 1,
                duplicateKey: "weather:" + weatherName,
                duplicateCooldownSeconds: 60);
        }

        public void RecordDayNightChanged(bool isNight)
        {
            if (_current == null || !_config.TrackEnvironment.Value)
            {
                return;
            }

            if (isNight)
            {
                _current.Environment.NightTransitions++;
            }
            else
            {
                _current.Environment.DayTransitions++;
            }

            _events.Add(
                EventTypes.DayNightChanged,
                EventCategories.Environment,
                isNight ? "Nastala noc." : "Začal den.",
                target: isNight ? "Night" : "Day",
                importance: 1,
                duplicateKey: "daynight:" + isNight,
                duplicateCooldownSeconds: 60);
        }

        public void RecordCombatDamage(Character victim, object hitData)
        {
            if (_current == null || !_config.TrackCombat.Value || victim == null || hitData == null)
            {
                return;
            }

            Character attacker = ValheimNames.GetHitAttacker(hitData);
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                return;
            }

            string localName = ValheimNames.GetPlayerName(localPlayer);

            if (attacker == localPlayer && victim != localPlayer && !ValheimNames.IsPlayer(victim))
            {
                _recentLocalDamageTargets[victim.GetInstanceID()] = DateTime.UtcNow;
                PlayerStats stats = EnsurePlayerStats(localName);
                stats.CombatMoments++;

                string enemyName = ValheimNames.GetCharacterName(victim);
                _events.Add(
                    EventTypes.CombatMoment,
                    EventCategories.Combat,
                    $"{localName} bojuje s {enemyName}.",
                    actor: localName,
                    target: enemyName,
                    position: ValheimNames.FormatPosition(victim.transform.position),
                    importance: 1,
                    duplicateKey: "combat:attack:" + enemyName,
                    duplicateCooldownSeconds: 180);
            }
            else if (victim == localPlayer && attacker != null && attacker != localPlayer)
            {
                PlayerStats stats = EnsurePlayerStats(localName);
                stats.CombatMoments++;

                string attackerName = ValheimNames.GetCharacterName(attacker);
                _events.Add(
                    EventTypes.CombatMoment,
                    EventCategories.Combat,
                    $"{localName} je v boji s {attackerName}.",
                    actor: localName,
                    target: attackerName,
                    position: ValheimNames.FormatPosition(localPlayer.transform.position),
                    importance: 1,
                    duplicateKey: "combat:defense:" + attackerName,
                    duplicateCooldownSeconds: 180);
            }
        }

        public void RecordCharacterDeath(Character character)
        {
            if (_current == null || character == null)
            {
                return;
            }

            if (character is Player player)
            {
                RecordPlayerDeath(player);
                return;
            }

            if (!_config.TrackCombat.Value)
            {
                return;
            }

            string characterName = ValheimNames.GetCharacterName(character);
            if (ValheimNames.IsBoss(character))
            {
                RecordBossKill(characterName, character);
                return;
            }

            if (ValheimNames.ShouldIgnoreEnemy(character))
            {
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            string localName = ValheimNames.GetPlayerName(localPlayer);
            bool likelyLocalKill = WasRecentlyDamagedByLocalPlayer(character);

            // Valheim does not expose server-authoritative kill ownership to this passive client.
            // A kill is attributed to the local player only when local damage was seen shortly before death.
            if (likelyLocalKill)
            {
                PlayerStats stats = EnsurePlayerStats(localName);
                stats.EnemiesKilled++;
                Increment(stats.EnemyKills, characterName);
            }
            else
            {
                _current.Environment.VisibleEnemyDeaths++;
            }

            _events.Add(
                EventTypes.EnemyKilled,
                EventCategories.Combat,
                likelyLocalKill
                    ? $"{localName} zabil {characterName}."
                    : $"V okolí zemřel nepřítel {characterName}.",
                actor: likelyLocalKill ? localName : "Okolí",
                target: characterName,
                position: ValheimNames.FormatPosition(character.transform.position),
                importance: likelyLocalKill ? 2 : 1,
                duplicateKey: "enemydeath:" + characterName,
                duplicateCooldownSeconds: likelyLocalKill ? 20 : 60);
        }

        public void RecordBossKill(string bossName, Character character)
        {
            if (_current == null)
            {
                return;
            }

            bossName = ValheimNames.NormalizeBossName(bossName);
            string localName = ValheimNames.GetLocalPlayerName();
            PlayerStats stats = EnsurePlayerStats(localName);
            stats.BossesKilled++;
            Increment(stats.BossKills, bossName);
            AddUnique(_current.Environment.BossesKilled, bossName);

            // Boss progression is detected from visible boss death objects, not from server state.
            _events.Add(
                EventTypes.BossKilled,
                EventCategories.Progression,
                $"Boss {bossName} byl poražen.",
                actor: "Skupina",
                target: bossName,
                position: character == null ? string.Empty : ValheimNames.FormatPosition(character.transform.position),
                importance: 5,
                duplicateKey: "boss:" + bossName,
                duplicateCooldownSeconds: 120);
        }

        public void RecordPortalUse(object portal, object[] args)
        {
            if (_current == null)
            {
                return;
            }

            Player player = ValheimNames.FindPlayerArgument(args) ?? Player.m_localPlayer;
            if (player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("portal:" + playerName, TimeSpan.FromSeconds(5)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            stats.PortalUses++;
            _current.Environment.PortalUses++;

            string portalTag = ValheimNames.GetPortalTag(portal);
            _events.Add(
                EventTypes.PortalUsed,
                EventCategories.Travel,
                string.IsNullOrWhiteSpace(portalTag)
                    ? $"{playerName} použil portál."
                    : $"{playerName} použil portál '{portalTag}'.",
                actor: playerName,
                target: portalTag,
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: 2,
                duplicateKey: "portal:" + playerName,
                duplicateCooldownSeconds: 5);
        }

        public void RecordSleeping(Player player)
        {
            if (_current == null || player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("sleep:" + playerName, TimeSpan.FromSeconds(30)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            stats.SleepingEvents++;

            _events.Add(
                EventTypes.Sleeping,
                EventCategories.Player,
                $"{playerName} si lehl ke spánku.",
                actor: playerName,
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: 2,
                duplicateKey: "sleep:" + playerName,
                duplicateCooldownSeconds: 60);
        }

        public void RecordShipUse(object[] args)
        {
            if (_current == null)
            {
                return;
            }

            Player player = ValheimNames.FindPlayerArgument(args) ?? Player.m_localPlayer;
            if (player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("ship:" + playerName, TimeSpan.FromSeconds(60)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            stats.ShipUses++;

            _events.Add(
                EventTypes.ShipUsed,
                EventCategories.Travel,
                $"{playerName} použil kormidlo lodi.",
                actor: playerName,
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: 2,
                duplicateKey: "ship:" + playerName,
                duplicateCooldownSeconds: 180);
        }

        public void RecordCrafting(Player player, object[] args)
        {
            if (_current == null || !_config.TrackCrafting.Value || player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            string craftedItem = ValheimNames.ExtractRecipeName(args);
            if (string.IsNullOrWhiteSpace(craftedItem))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("craft:" + playerName + ":" + craftedItem, TimeSpan.FromSeconds(1)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            bool firstCraft = !stats.CraftedItems.ContainsKey(craftedItem);
            stats.Crafts++;
            Increment(stats.CraftedItems, craftedItem);

            if (firstCraft || ValheimNames.IsImportantCraftingMilestone(craftedItem))
            {
                _events.Add(
                    firstCraft ? EventTypes.Discovery : EventTypes.Crafting,
                    firstCraft ? EventCategories.Progression : EventCategories.Crafting,
                    firstCraft
                        ? $"{playerName} poprvé vyrobil {craftedItem}."
                        : $"{playerName} vyrobil {craftedItem}.",
                    actor: playerName,
                    target: craftedItem,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: firstCraft ? 3 : 2,
                    duplicateKey: "craft:" + craftedItem,
                    duplicateCooldownSeconds: 30);
            }
        }

        public void RecordPiecePlacement(Player player, object[] args)
        {
            if (_current == null || !_config.TrackBuilding.Value || player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            object piece = ValheimNames.FindPieceArgument(args);
            string pieceName = ValheimNames.GetPieceName(piece);
            if (string.IsNullOrWhiteSpace(pieceName))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("piece:" + playerName + ":" + pieceName, TimeSpan.FromSeconds(1)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            bool firstPlacement = !stats.PiecesPlaced.ContainsKey(pieceName);
            bool workstation = ValheimNames.IsWorkstationPiece(piece, pieceName);

            stats.PiecesPlacedTotal++;
            Increment(stats.PiecesPlaced, pieceName);
            if (workstation)
            {
                stats.WorkstationsPlaced++;
            }

            if (firstPlacement || workstation || ValheimNames.IsImportantPiece(pieceName))
            {
                _events.Add(
                    workstation ? EventTypes.WorkstationPlaced : EventTypes.PiecePlaced,
                    workstation ? EventCategories.Progression : EventCategories.Building,
                    workstation
                        ? $"{playerName} postavil workstation {pieceName}."
                        : $"{playerName} postavil {pieceName}.",
                    actor: playerName,
                    target: pieceName,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: workstation ? 3 : 2,
                    duplicateKey: "piece:" + pieceName,
                    duplicateCooldownSeconds: workstation ? 10 : 60);
            }
        }

        public void RecordItemPickup(Humanoid humanoid, object[] args)
        {
            if (_current == null || humanoid == null)
            {
                return;
            }

            Player player = humanoid as Player;
            if (player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            string itemName = ValheimNames.ExtractItemName(args);
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("pickup:" + playerName + ":" + itemName, TimeSpan.FromSeconds(0.5)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            bool firstPickup = !stats.ItemPickups.ContainsKey(itemName);
            stats.ItemsPickedUp++;
            Increment(stats.ItemPickups, itemName);

            if (firstPickup || ValheimNames.IsImportantItem(itemName))
            {
                _events.Add(
                    firstPickup ? EventTypes.Discovery : EventTypes.ItemPickedUp,
                    firstPickup ? EventCategories.Progression : EventCategories.Loot,
                    firstPickup
                        ? $"{playerName} poprvé sebral {itemName}."
                        : $"{playerName} sebral {itemName}.",
                    actor: playerName,
                    target: itemName,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: firstPickup ? 2 : 1,
                    duplicateKey: "pickup:" + itemName,
                duplicateCooldownSeconds: 120);
            }
        }

        public void RecordItemDropPickup(ItemDrop itemDrop, object[] args)
        {
            if (_current == null || itemDrop == null)
            {
                return;
            }

            Player player = ValheimNames.FindPlayerArgument(args) ?? Player.m_localPlayer;
            if (player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            RecordItemPickup(player, new object[] { itemDrop });
        }

        public void RecordTombstoneCreated(Player player)
        {
            if (_current == null || player == null || !ValheimNames.IsLocalPlayer(player))
            {
                return;
            }

            string playerName = ValheimNames.GetPlayerName(player);
            if (!ShouldCount("tombstone:" + playerName, TimeSpan.FromSeconds(20)))
            {
                return;
            }

            PlayerStats stats = EnsurePlayerStats(playerName);
            stats.TombstonesCreated++;

            _events.Add(
                EventTypes.TombstoneCreated,
                EventCategories.Player,
                $"Byl vytvořen náhrobek hráče {playerName}.",
                actor: playerName,
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: 3,
                duplicateKey: "tombstone:" + playerName,
                duplicateCooldownSeconds: 20);
        }

        private PlayerStats EnsurePlayerStats(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Neznámý hráč";
            }

            if (!_current.PlayerStats.TryGetValue(playerName, out PlayerStats stats))
            {
                stats = new PlayerStats { PlayerName = playerName };
                _current.PlayerStats[playerName] = stats;
            }

            return stats;
        }

        private bool WasRecentlyDamagedByLocalPlayer(Character character)
        {
            int id = character.GetInstanceID();
            if (!_recentLocalDamageTargets.TryGetValue(id, out DateTime when))
            {
                return false;
            }

            bool recent = DateTime.UtcNow - when <= RecentLocalDamageWindow;
            _recentLocalDamageTargets.Remove(id);
            return recent;
        }

        private static bool AddUnique(ICollection<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            values.Add(value);
            return true;
        }

        private static void Increment(IDictionary<string, int> counts, string key, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            counts.TryGetValue(key, out int current);
            counts[key] = current + amount;
        }

        private bool ShouldCount(string key, TimeSpan cooldown)
        {
            DateTime now = DateTime.UtcNow;
            if (_lastCountByKey.TryGetValue(key, out DateTime last) && now - last < cooldown)
            {
                return false;
            }

            _lastCountByKey[key] = now;
            return true;
        }
    }
}
