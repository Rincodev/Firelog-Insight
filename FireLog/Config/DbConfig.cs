namespace FireLog.Config;

public sealed class DbConfig
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 5432;
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public string Database { get; init; } = "postgres";
    public string SslMode { get; init; } = "VerifyFull";
}
