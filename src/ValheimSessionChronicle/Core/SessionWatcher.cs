using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Core
{
    public sealed class SessionWatcher : MonoBehaviour
    {
        private SessionManager _sessionManager;
        private SessionLifecycleManager _lifecycleManager;
        private ChronicleConfig _config;
        private float _nextSessionCheck;
        private float _nextPlayersCheck;
        private float _nextBiomeCheck;
        private float _nextEnvironmentCheck;
        private float _nextProgressionScan;
        private string _lastBiome;
        private string _lastWeather;
        private bool? _lastNightState;

        public void Initialize(SessionManager sessionManager, SessionLifecycleManager lifecycleManager, ChronicleConfig config)
        {
            _sessionManager = sessionManager;
            _lifecycleManager = lifecycleManager;
            _config = config;
        }

        private void Update()
        {
            if (_sessionManager == null || _lifecycleManager == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= _nextSessionCheck)
            {
                _nextSessionCheck = now + 1f;
                CheckSessionState();
            }

            if (!_sessionManager.IsActive)
            {
                return;
            }

            if (now >= _nextPlayersCheck)
            {
                _nextPlayersCheck = now + 5f;
                CheckVisiblePlayers();
            }

            if (_config.TrackEnvironment.Value && now >= _nextBiomeCheck)
            {
                _nextBiomeCheck = now + 2f;
                CheckBiome();
            }

            if (_config.TrackEnvironment.Value && now >= _nextEnvironmentCheck)
            {
                _nextEnvironmentCheck = now + 8f;
                CheckEnvironment();
            }

            if (now >= _nextProgressionScan)
            {
                _nextProgressionScan = now + 45f;
                CheckProgressionContext();
            }
        }

        private void CheckSessionState()
        {
            try
            {
                bool wasActive = _sessionManager.IsActive;
                _lifecycleManager.ObserveWorldState();

                if (!wasActive && _sessionManager.IsActive)
                {
                    _lastBiome = null;
                    _lastWeather = null;
                    _lastNightState = null;
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Session watcher failed.");
            }
        }

        private void CheckVisiblePlayers()
        {
            try
            {
                IEnumerable players = ValheimNames.GetAllKnownPlayers();
                foreach (object entry in players)
                {
                    if (entry is Player player)
                    {
                        _sessionManager.RecordVisiblePlayer(player);
                    }
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Visible player scan failed: {ex.Message}");
            }
        }

        private void CheckBiome()
        {
            try
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null)
                {
                    return;
                }

                string biome = ValheimNames.GetCurrentBiomeName(localPlayer);
                if (!string.IsNullOrWhiteSpace(biome) &&
                    !string.Equals(biome, _lastBiome, StringComparison.OrdinalIgnoreCase))
                {
                    _lastBiome = biome;
                    _sessionManager.RecordBiomeEntered(ValheimNames.GetPlayerName(localPlayer), biome, localPlayer.transform.position);
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Biome tracking failed: {ex.Message}");
            }
        }

        private void CheckEnvironment()
        {
            try
            {
                string weather = ValheimNames.GetCurrentWeatherName();
                if (!string.IsNullOrWhiteSpace(weather) &&
                    !string.Equals(weather, _lastWeather, StringComparison.OrdinalIgnoreCase))
                {
                    _lastWeather = weather;
                    _sessionManager.RecordWeatherChanged(weather);
                }

                bool? isNight = ValheimNames.IsNight();
                if (isNight.HasValue && (!_lastNightState.HasValue || isNight.Value != _lastNightState.Value))
                {
                    _lastNightState = isNight.Value;
                    _sessionManager.RecordDayNightChanged(isNight.Value);
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Environment tracking failed: {ex.Message}");
            }
        }

        private void CheckProgressionContext()
        {
            try
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null)
                {
                    return;
                }

                _sessionManager.RecordInventoryObservation(ValheimNames.GetInventoryItemCounts(localPlayer));
                _sessionManager.RecordContainerObservation(ScanNearbyContainers(localPlayer));
                ScanNearbyCraftingStations(localPlayer);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Progression context scan failed: {ex.Message}");
            }
        }

        private static Dictionary<string, int> ScanNearbyContainers(Player localPlayer)
        {
            const float Radius = 32f;
            const int MaxContainersPerScan = 40;

            Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int scanned = 0;

            foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
            {
                if (container == null || scanned >= MaxContainersPerScan)
                {
                    break;
                }

                if ((container.transform.position - localPlayer.transform.position).sqrMagnitude > Radius * Radius)
                {
                    continue;
                }

                scanned++;
                Dictionary<string, int> containerItems = ValheimNames.GetInventoryItemCounts(container);
                foreach (KeyValuePair<string, int> pair in containerItems)
                {
                    totals.TryGetValue(pair.Key, out int current);
                    totals[pair.Key] = current + pair.Value;
                }
            }

            return totals;
        }

        private void ScanNearbyCraftingStations(Player localPlayer)
        {
            const float Radius = 64f;
            const int MaxStationsPerScan = 80;

            int scanned = 0;
            foreach (CraftingStation station in UnityEngine.Object.FindObjectsByType<CraftingStation>(FindObjectsSortMode.None))
            {
                if (station == null || scanned >= MaxStationsPerScan)
                {
                    break;
                }

                if ((station.transform.position - localPlayer.transform.position).sqrMagnitude > Radius * Radius)
                {
                    continue;
                }

                scanned++;
                Piece piece = station.GetComponent<Piece>();
                object stationObject = piece != null ? (object)piece : station;
                string stationName = ValheimNames.GetPieceName(stationObject);
                if (!IsProgressionStation(stationName))
                {
                    continue;
                }

                _sessionManager.RecordProgressionStructureObservation(stationName, "CraftingStation", station.transform.position);
            }
        }

        private static bool IsProgressionStation(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return false;
            }

            return ValheimNames.IsImportantPiece(stationName) ||
                   ChronicleFilters.IsForgePiece(stationName) ||
                   ChronicleFilters.IsAdvancedStationPiece(stationName);
        }
    }
}
