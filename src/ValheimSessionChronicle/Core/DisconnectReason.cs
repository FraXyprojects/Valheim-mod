namespace ValheimSessionChronicle.Core
{
    public enum DisconnectReason
    {
        Unknown,
        WatcherLostSession,
        ZNetShutdown,
        GameLogout,
        ApplicationQuit,
        PluginUnload
    }
}
