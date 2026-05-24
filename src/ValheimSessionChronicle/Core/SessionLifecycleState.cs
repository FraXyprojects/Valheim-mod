namespace ValheimSessionChronicle.Core
{
    public enum SessionLifecycleState
    {
        Idle,
        Connecting,
        Connected,
        InWorld,
        TemporaryConnectionLoss,
        Disconnecting,
        Disconnected
    }
}
