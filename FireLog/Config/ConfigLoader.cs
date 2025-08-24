using System.Text.Json;
using System;
using System.IO;
namespace FireLog.Config;

public static class ConfigLoader
{
    public static DbConfig Load(string? jsonPath = null)
    {
        var env = Environment.GetEnvironmentVariables();
        string? Get(string k) => env[k] as string;

        var cfg = new DbConfig
        {
            Host = Get("APP_DB_HOST") ?? "",
            Port = int.TryParse(Get("APP_DB_PORT"), out var p) ? p : 5432,
            User = Get("APP_DB_USER") ?? "",
            Password = Get("APP_DB_PASSWORD") ?? "",
            Database = Get("APP_DB_NAME") ?? "postgres",
            SslMode = Get("APP_DB_SSLMODE") ?? "VerifyFull"
        };

        if (string.IsNullOrWhiteSpace(cfg.Host))
        {
            var file = jsonPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_credentials.json");
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                var fromFile = JsonSerializer.Deserialize<DbConfig>(json);
                if (fromFile != null) cfg = fromFile;
            }
        }
        return cfg;
    }

    public static bool IsFilled(DbConfig c) =>
        !string.IsNullOrWhiteSpace(c.Host) &&
        !string.IsNullOrWhiteSpace(c.User) &&
        !string.IsNullOrWhiteSpace(c.Database);
}
