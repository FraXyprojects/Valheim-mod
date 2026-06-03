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

        public void TouchNetwork(DateTime timestampUtc)
        {
            if (_current != null)
            {
                _current.LastConfirmedNetworkUtc = timestampUtc;
            }
        }

        public void TouchWorld(DateTime timestampUtc)
        {
            if (_current != null)
            {
                _current.LastWorldActivityUtc = timestampUtc;
                _current.LastConfirmedNetworkUtc = timestampUtc;
            }
        }

        public void RegisterReconnect(DateTime timestampUtc, string reason)
        {
            if (_current == null)
            {
                return;
            }

            _current.ReconnectCount++;
            TouchWorld(timestampUtc);
            ChronicleLogger.Info($"Session recovered after temporary connection loss. Reconnects={_current.ReconnectCount}. {reason}");
        }

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
                LastWorldActivityUtc = DateTime.UtcNow,
                LastConfirmedNetworkUtc = DateTime.UtcNow,
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
                importance: EventImportance.High);

            _events.Add(
                EventTypes.PlayerJoined,
                EventCategories.Player,
                $"{localPlayer} se připojil k serveru.",
                actor: localPlayer,
                importance: EventImportance.Medium);

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
                    importance: EventImportance.High,
                    metadata: new Dictionary<string, string> { ["DisconnectReason"] = disconnectReason.ToString() });

                StorageResult result = _storage.Save(_current, _config);

                if (_config.EnableDiscordWebhook.Value && !string.IsNullOrWhiteSpace(_config.DiscordWebhookURL.Value))
                {
                    _discordWebhookClient.SendPlainTextAsync(_config.DiscordWebhookURL.Value, result.TxtReport ?? string.Empty);
                }

                ChronicleLogger.Info(string.IsNullOrWhiteSpace(result.JsonPath)
                    ? $"Session ended. TXT='{result.TxtPath}', world memory='{result.WorldMemoryPath}'."
                    : $"Session ended. TXT='{result.TxtPath}', debug JSON='{result.JsonPath}', world memory='{result.WorldMemoryPath}'.");
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
            _current.CombatSamples.Add(new CombatActivitySample
            {
                TimestampUtc = DateTime.UtcNow,
                Actor = playerName,
                Biome = GetLocalBiome(),
                CausedDeath = true,
                HealthAfterHit = 0f
            });

            _events.Add(
                EventTypes.PlayerDeath,
                EventCategories.Player,
                $"{playerName} zemřel.",
                actor: playerName,
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: EventImportance.High,
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
                    importance: EventImportance.Medium,
                    duplicateKey: "respawn:" + playerName,
                    duplicateCooldownSeconds: 10);
            }
        }

        public void RecordBiomeEntered(string playerName, string biomeName, Vector3 position)
        {
            if (_current == null || !ChronicleFilters.IsValidBiome(biomeName))
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
                    importance: firstForSession ? EventImportance.High : EventImportance.Medium,
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
                importance: EventImportance.Low,
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
                importance: EventImportance.Low,
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
            PlayerStats stats = EnsurePlayerStats(localName);

            if (attacker == localPlayer && victim != localPlayer && !ValheimNames.IsPlayer(victim))
            {
                _recentLocalDamageTargets[victim.GetInstanceID()] = DateTime.UtcNow;
                stats.CombatMoments++;
            }
            else if (victim == localPlayer && attacker != null && attacker != localPlayer)
            {
                stats.CombatMoments++;

                string attackerName = ValheimNames.GetCharacterName(attacker);
                float health = ValheimNames.GetCharacterHealth(localPlayer);
                float maxHealth = ValheimNames.GetCharacterMaxHealth(localPlayer);
                float incomingDamage = ValheimNames.GetHitTotalDamage(hitData);
                bool nearDeath = IsNearDeath(health, maxHealth);
                string biome = GetLocalBiome();

                _current.CombatSamples.Add(new CombatActivitySample
                {
                    TimestampUtc = DateTime.UtcNow,
                    Actor = localName,
                    EnemyName = attackerName,
                    Biome = biome,
                    IsEliteEnemy = ChronicleFilters.IsDangerousEnemy(attackerName),
                    IsIncomingDamage = true,
                    IncomingDamage = incomingDamage,
                    HealthAfterHit = health,
                    MaxHealth = maxHealth,
                    IsNearDeath = nearDeath
                });

                if (ShouldCount("danger:" + attackerName, TimeSpan.FromSeconds(90)))
                {
                    stats.DangerousEncounters++;
                    if (ChronicleFilters.IsValidBiome(biome))
                    {
                        Increment(stats.DangerousEncountersByBiome, biome);
                    }
                }
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

            // Kill ownership is inferred only from recent local damage. Unclaimed visible deaths stay out of the chronicle.
            if (likelyLocalKill)
            {
                PlayerStats stats = EnsurePlayerStats(localName);
                stats.EnemiesKilled++;
                Increment(stats.EnemyKills, characterName);

                string biome = GetLocalBiome();
                if (ChronicleFilters.IsValidBiome(biome))
                {
                    IncrementNested(stats.EnemyKillsByBiome, biome, characterName);
                }

                int killCount = stats.EnemyKills[characterName];
                bool dangerousEnemy = ChronicleFilters.IsDangerousEnemy(characterName);
                _current.CombatSamples.Add(new CombatActivitySample
                {
                    TimestampUtc = DateTime.UtcNow,
                    Actor = localName,
                    EnemyName = characterName,
                    Biome = biome,
                    IsKill = true,
                    IsEliteEnemy = dangerousEnemy
                });

                if (IsMilestoneCount(killCount) || (dangerousEnemy && killCount == 1))
                {
                    _events.Add(
                        EventTypes.CombatMilestone,
                        EventCategories.Combat,
                        BuildCombatMilestoneDescription(localName, characterName, killCount, biome, dangerousEnemy),
                        actor: localName,
                        target: characterName,
                        biome: ChronicleFilters.IsValidBiome(biome) ? biome : string.Empty,
                        position: ValheimNames.FormatPosition(character.transform.position),
                        importance: dangerousEnemy ? EventImportance.High : EventImportance.Medium,
                        duplicateKey: "combatmilestone:" + characterName + ":" + killCount,
                        duplicateCooldownSeconds: 5);
                }
            }
            else
            {
                _current.Environment.VisibleEnemyDeaths++;
            }
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
            _current.CombatSamples.Add(new CombatActivitySample
            {
                TimestampUtc = DateTime.UtcNow,
                Actor = localName,
                EnemyName = bossName,
                IsKill = true,
                IsEliteEnemy = true,
                IsBoss = true,
                Biome = GetLocalBiome()
            });

            // Boss progression is detected from visible boss death objects, not from server state.
            _events.Add(
                EventTypes.BossKilled,
                EventCategories.Progression,
                $"Boss {bossName} byl poražen.",
                actor: "Skupina",
                target: bossName,
                position: character == null ? string.Empty : ValheimNames.FormatPosition(character.transform.position),
                importance: EventImportance.Critical,
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
            string biome = GetLocalBiome();
            _current.PortalSamples.Add(new PortalActivitySample
            {
                TimestampUtc = DateTime.UtcNow,
                PortalName = portalTag,
                Biome = biome,
                X = player.transform.position.x,
                Y = player.transform.position.y,
                Z = player.transform.position.z
            });

            bool firstPortalUse = stats.PortalUses == 1;
            bool namedPortalUse = !string.IsNullOrWhiteSpace(portalTag) &&
                                  ShouldCount("portal-tag:" + portalTag, TimeSpan.FromMinutes(10));

            if (firstPortalUse || namedPortalUse)
            {
                _events.Add(
                    EventTypes.PortalUsed,
                    EventCategories.Travel,
                    string.IsNullOrWhiteSpace(portalTag)
                    ? $"{playerName} poprvé použil portál."
                    : $"{playerName} použil portál '{portalTag}' jako důležitý přesun výpravy.",
                    actor: playerName,
                    target: portalTag,
                    biome: biome,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: EventImportance.Medium,
                    duplicateKey: "portal-event:" + playerName + ":" + portalTag,
                    duplicateCooldownSeconds: 300);
            }
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

            if (stats.SleepingEvents == 1)
            {
                _events.Add(
                    EventTypes.Sleeping,
                    EventCategories.Player,
                    $"{playerName} si poprvé během session odpočinul u postele.",
                    actor: playerName,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: EventImportance.Medium,
                    duplicateKey: "sleep-event:" + playerName,
                    duplicateCooldownSeconds: 300);
            }
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

            if (stats.ShipUses == 1)
            {
                _events.Add(
                    EventTypes.ShipUsed,
                    EventCategories.Travel,
                    $"{playerName} vyrazil na vodu a převzal kormidlo lodi.",
                    actor: playerName,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: EventImportance.High,
                    duplicateKey: "ship-event:" + playerName,
                    duplicateCooldownSeconds: 600);
            }
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

            if (firstCraft && ValheimNames.IsImportantCraftingMilestone(craftedItem))
            {
                _events.Add(
                    EventTypes.Crafting,
                    EventCategories.Progression,
                    $"{playerName} vyrobil důležitý předmět: {craftedItem}.",
                    actor: playerName,
                    target: craftedItem,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: EventImportance.High,
                    duplicateKey: "craft-event:" + craftedItem,
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
            Vector3 buildPosition = GetPiecePosition(piece, player.transform.position);

            stats.PiecesPlacedTotal++;
            Increment(stats.PiecesPlaced, pieceName);
            if (workstation)
            {
                stats.WorkstationsPlaced++;
            }

            string biome = GetLocalBiome();
            _current.BuildSamples.Add(new BuildActivitySample
            {
                TimestampUtc = DateTime.UtcNow,
                PlayerName = playerName,
                PieceName = pieceName,
                Biome = biome,
                X = buildPosition.x,
                Y = buildPosition.y,
                Z = buildPosition.z,
                IsFire = ChronicleFilters.IsFirePiece(pieceName),
                IsWorkbench = ChronicleFilters.IsWorkbenchPiece(pieceName),
                IsBed = ChronicleFilters.IsBedPiece(pieceName),
                IsStorage = ChronicleFilters.IsStoragePiece(pieceName),
                IsWallOrDefense = ChronicleFilters.IsWallOrDefensePiece(pieceName),
                IsPortal = ChronicleFilters.IsPortalPiece(pieceName),
                IsForge = ChronicleFilters.IsForgePiece(pieceName),
                IsAdvancedStation = ChronicleFilters.IsAdvancedStationPiece(pieceName),
                IsWorkstation = workstation
            });

            bool firstCampInBiome = false;
            if (ChronicleFilters.IsCampPiece(pieceName) && ChronicleFilters.IsValidBiome(biome))
            {
                firstCampInBiome = AddUnique(_current.Environment.OutpostBiomes, biome);
            }

            if (ChronicleFilters.TryGetBuildingMilestone(
                    pieceName,
                    biome,
                    firstPlacement,
                    firstCampInBiome,
                    out string description,
                    out EventImportance importance))
            {
                _events.Add(
                    EventTypes.BuildingMilestone,
                    EventCategories.Building,
                    description,
                    actor: playerName,
                    target: pieceName,
                    biome: ChronicleFilters.IsValidBiome(biome) ? biome : string.Empty,
                    position: ValheimNames.FormatPosition(player.transform.position),
                    importance: importance,
                    duplicateKey: "buildingmilestone:" + pieceName + ":" + biome,
                    duplicateCooldownSeconds: 300);
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

            if (ChronicleFilters.IsCommonResource(itemName) || !firstPickup || !ValheimNames.IsImportantItem(itemName))
            {
                return;
            }

            _events.Add(
                EventTypes.Discovery,
                EventCategories.Progression,
                $"{playerName} objevil důležitý nález: {itemName}.",
                actor: playerName,
                target: itemName,
                biome: GetLocalBiome(),
                position: ValheimNames.FormatPosition(player.transform.position),
                importance: EventImportance.High,
                duplicateKey: "pickup-event:" + itemName,
                duplicateCooldownSeconds: 120);
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

        public void RecordInventoryObservation(Dictionary<string, int> itemCounts)
        {
            if (_current == null || itemCounts == null || itemCounts.Count == 0)
            {
                return;
            }

            MergeMax(_current.ObservedInventoryItems, itemCounts);
        }

        public void RecordContainerObservation(Dictionary<string, int> itemCounts)
        {
            if (_current == null || itemCounts == null || itemCounts.Count == 0)
            {
                return;
            }

            MergeMax(_current.ObservedContainerItems, itemCounts);
        }

        public void RecordProgressionStructureObservation(string structureName, string structureType, Vector3 position)
        {
            if (_current == null || string.IsNullOrWhiteSpace(structureName) || _current.StructureObservations.Count >= 300)
            {
                return;
            }

            string normalized = ChronicleFilters.NormalizeKey(structureName);
            bool alreadySeen = _current.StructureObservations.Any(existing =>
                ChronicleFilters.NormalizeKey(existing.StructureName) == normalized &&
                DistanceSquared(existing.X, existing.Z, position.x, position.z) <= 35f * 35f);

            if (alreadySeen)
            {
                return;
            }

            _current.StructureObservations.Add(new ProgressionStructureObservation
            {
                TimestampUtc = DateTime.UtcNow,
                StructureName = structureName,
                StructureType = structureType,
                Biome = GetLocalBiome(),
                X = position.x,
                Y = position.y,
                Z = position.z
            });
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
                importance: EventImportance.High,
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

        private string GetLocalBiome()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                return string.Empty;
            }

            string biome = ValheimNames.GetCurrentBiomeName(localPlayer);
            return ChronicleFilters.IsValidBiome(biome) ? biome : string.Empty;
        }

        private static string BuildCombatMilestoneDescription(string playerName, string enemyName, int killCount, string biome, bool dangerousEnemy)
        {
            string place = ChronicleFilters.IsValidBiome(biome) ? $" v biomu {biome}" : string.Empty;
            if (dangerousEnemy && killCount == 1)
            {
                return $"{playerName} porazil nebezpečného protivníka {enemyName}{place}.";
            }

            return $"{playerName} během výpravy porazil už {killCount}x {enemyName}{place}.";
        }

        private static bool IsMilestoneCount(int count)
        {
            return count == 5 || count == 10 || count == 25 || count == 50 || count % 100 == 0;
        }

        private static bool IsNearDeath(float health, float maxHealth)
        {
            if (health <= 0f)
            {
                return false;
            }

            if (maxHealth > 0f && health / maxHealth <= 0.15f)
            {
                return true;
            }

            return health <= 20f;
        }

        private static Vector3 GetPiecePosition(object piece, Vector3 fallback)
        {
            if (piece is Component component)
            {
                return component.transform.position;
            }

            return fallback;
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

        private static void IncrementNested(IDictionary<string, Dictionary<string, int>> counts, string group, string key)
        {
            if (string.IsNullOrWhiteSpace(group) || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!counts.TryGetValue(group, out Dictionary<string, int> nested))
            {
                nested = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                counts[group] = nested;
            }

            Increment(nested, key);
        }

        private static void MergeMax(IDictionary<string, int> target, IEnumerable<KeyValuePair<string, int>> incoming)
        {
            foreach (KeyValuePair<string, int> pair in incoming)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                {
                    continue;
                }

                target.TryGetValue(pair.Key, out int current);
                target[pair.Key] = Math.Max(current, pair.Value);
            }
        }

        private static float DistanceSquared(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return dx * dx + dz * dz;
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
