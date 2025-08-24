// FILE: DbHelper.cs
using System;
using System.IO;
using Newtonsoft.Json;
using Npgsql;

public class DbCredentials
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 5432;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string Database { get; set; } = "postgres";
    public string SslMode { get; set; } = "VerifyFull"; // default safer than Require
}

public static class DbHelper
{
    public static string GetConnectionString()
    {
        // ENV has priority
        var host = Environment.GetEnvironmentVariable("APP_DB_HOST");
        if (!string.IsNullOrWhiteSpace(host))
        {
            var csbEnv = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = TryInt(Environment.GetEnvironmentVariable("APP_DB_PORT"), 5432),
                Username = Environment.GetEnvironmentVariable("APP_DB_USER") ?? "",
                Password = Environment.GetEnvironmentVariable("APP_DB_PASSWORD") ?? "",
                Database = Environment.GetEnvironmentVariable("APP_DB_NAME") ?? "postgres",
                SslMode = ParseSslMode(Environment.GetEnvironmentVariable("APP_DB_SSLMODE") ?? "VerifyFull"),
                Timeout = 15,
                CommandTimeout = 30,
                KeepAlive = 60,
            };
            return csbEnv.ConnectionString;
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var file = Path.Combine(baseDir, "db_credentials.json");
        if (!File.Exists(file))
            throw new FileNotFoundException("db_credentials.json not found and APP_DB_* variables are not set.", file);

        var json = File.ReadAllText(file);
        var creds = JsonConvert.DeserializeObject<DbCredentials>(json) ?? new DbCredentials();

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = creds.Host,
            Port = creds.Port,
            Username = creds.User,
            Password = creds.Password,
            Database = creds.Database,
            SslMode = ParseSslMode(creds.SslMode ?? "VerifyFull"),
            Timeout = 15,
            CommandTimeout = 30,
            KeepAlive = 60,
        };

        return csb.ConnectionString;
    }

    private static int TryInt(string? s, int fallback) =>
        int.TryParse(s, out var v) ? v : fallback;

    private static SslMode ParseSslMode(string s)
    {
        if (Enum.TryParse<SslMode>(s, ignoreCase: true, out var mode))
            return mode;
        return SslMode.VerifyFull; // безопасный дефолт
    }
}
