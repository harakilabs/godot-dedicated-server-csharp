namespace GodotMultiplayer.Shared
{
    public static class GameSettings
    {
        public static bool IsServer { get; set; } = false;
        public static string ServerIP { get; set; } = DEFAULT_SERVER_IP;
        public const int SERVER_PORT = 7777;
        public const string DEFAULT_SERVER_IP = "127.0.0.1";
        public const int CONNECTION_TIMEOUT = 30;
    }
}
