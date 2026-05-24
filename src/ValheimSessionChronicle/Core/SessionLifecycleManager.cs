using System;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Core
{
    public sealed class SessionLifecycleManager
    {
        private readonly SessionManager _sessionManager;
        private readonly ChronicleConfig _config;

        private DateTime _lossStartedUtc;
        private DateTime _disconnectSignalUtc;
        private bool _wasInWorld;

        public SessionLifecycleManager(SessionManager sessionManager, ChronicleConfig config)
        {
            _sessionManager = sessionManager;
            _config = config;
            State = SessionLifecycleState.Idle;
        }

        public SessionLifecycleState State { get; private set; }

        public void ObserveWorldState()
        {
            DateTime now = DateTime.UtcNow;
            bool networkPresent = ValheimState.HasNetwork();
            bool playerPresent = ValheimState.HasLocalPlayer();
            bool inWorld = networkPresent && playerPresent;

            if (networkPresent)
            {
                _sessionManager.TouchNetwork(now);
            }

            if (inWorld)
            {
                _sessionManager.TouchWorld(now);
                HandleConfirmedWorld(now);
                return;
            }

            HandleMissingWorld(now, networkPresent);
        }

        public void NotifyDisconnectSignal(string reason, DisconnectReason disconnectReason)
        {
            if (!_sessionManager.IsActive)
            {
                State = SessionLifecycleState.Disconnected;
                return;
            }

            _disconnectSignalUtc = DateTime.UtcNow;
            Transition(SessionLifecycleState.Disconnecting, reason);
            BeginTemporaryLoss(reason, disconnectReason);
        }

        public void ForceFinalize(string reason, DisconnectReason disconnectReason)
        {
            if (_sessionManager.IsActive)
            {
                Transition(SessionLifecycleState.Disconnecting, reason);
                _sessionManager.EndSession(reason, disconnectReason);
            }

            Transition(SessionLifecycleState.Disconnected, reason);
            _wasInWorld = false;
        }

        private void HandleConfirmedWorld(DateTime now)
        {
            if (!_sessionManager.IsActive)
            {
                Transition(SessionLifecycleState.Connecting, "Playable world detected.");
                _sessionManager.StartSession("Lifecycle manager confirmed local player and network.");
                Transition(SessionLifecycleState.InWorld, "Session entered world.");
                _wasInWorld = true;
                return;
            }

            if (State == SessionLifecycleState.TemporaryConnectionLoss || State == SessionLifecycleState.Disconnecting)
            {
                _sessionManager.RegisterReconnect(now, "Temporary connection loss recovered.");
                Transition(SessionLifecycleState.InWorld, "Session recovered before reconnect timeout.");
            }
            else if (State != SessionLifecycleState.InWorld)
            {
                Transition(SessionLifecycleState.InWorld, "Playable world confirmed.");
            }

            _lossStartedUtc = default(DateTime);
            _disconnectSignalUtc = default(DateTime);
            _wasInWorld = true;
        }

        private void HandleMissingWorld(DateTime now, bool networkPresent)
        {
            if (!_sessionManager.IsActive)
            {
                Transition(networkPresent ? SessionLifecycleState.Connected : SessionLifecycleState.Idle, "No active chronicle session.");
                return;
            }

            if (_lossStartedUtc == default(DateTime))
            {
                _lossStartedUtc = now;
            }

            double missingSeconds = (now - _lossStartedUtc).TotalSeconds;
            if (missingSeconds < Math.Max(1, _config.DisconnectDebounceSeconds.Value))
            {
                return;
            }

            BeginTemporaryLoss("Playable world temporarily missing.", DisconnectReason.WatcherLostSession);

            double reconnectSeconds = (now - _lossStartedUtc).TotalSeconds;
            if (_disconnectSignalUtc != default(DateTime))
            {
                reconnectSeconds = Math.Max(reconnectSeconds, (now - _disconnectSignalUtc).TotalSeconds);
            }

            if (reconnectSeconds >= Math.Max(30, _config.ReconnectToleranceSeconds.Value))
            {
                _sessionManager.EndSession("Reconnect timeout expired; world stayed unloaded.", DisconnectReason.WatcherLostSession);
                Transition(SessionLifecycleState.Disconnected, "Reconnect timeout expired.");
                _lossStartedUtc = default(DateTime);
                _disconnectSignalUtc = default(DateTime);
                _wasInWorld = false;
            }
        }

        private void BeginTemporaryLoss(string reason, DisconnectReason disconnectReason)
        {
            if (!_wasInWorld && State != SessionLifecycleState.Disconnecting)
            {
                return;
            }

            if (State != SessionLifecycleState.TemporaryConnectionLoss && State != SessionLifecycleState.Disconnecting)
            {
                Transition(SessionLifecycleState.TemporaryConnectionLoss, reason);
            }
        }

        private void Transition(SessionLifecycleState nextState, string reason)
        {
            if (State == nextState)
            {
                return;
            }

            ChronicleLogger.Verbose($"Lifecycle {State} -> {nextState}: {reason}");
            State = nextState;
        }
    }
}
