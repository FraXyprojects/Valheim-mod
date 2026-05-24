using System;
using System.Collections;
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
    }
}
