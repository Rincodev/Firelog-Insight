using FireLog.Config;
using FireLog.Data;
using Npgsql;
    namespace FireLog
{
    public enum AppMode { Offline, Online }
    public static class AppState
    {
        public static Guid? CurrentUserId { get; set; }
        public static AppMode Mode { get; private set; } = AppMode.Offline;
        public static bool DbAvailable => Mode == AppMode.Online;
        public static async Task InitializeAsync(CancellationToken ct = default)
        {
            var cfg = ConfigLoader.Load();
            if (!ConfigLoader.IsFilled(cfg)) { Mode = AppMode.Offline; return; }

            try
            {
                using var conn = Db.CreateConnection(cfg);
                await conn.OpenAsync(ct);
                Mode = AppMode.Online;
            }
            catch
            {
                Mode = AppMode.Offline;
            }
        }
    }
}