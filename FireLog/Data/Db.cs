using FireLog.Config;
using Npgsql;

namespace FireLog.Data;

public static class Db
{
    public static NpgsqlConnection CreateConnection(DbConfig c)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = c.Host,
            Port = c.Port,
            Username = c.User,
            Password = c.Password,
            Database = c.Database,
            SslMode = Npgsql.SslMode.VerifyFull,
            Timeout = 15,
            CommandTimeout = 30,
            KeepAlive = 60
        };
        return new NpgsqlConnection(csb.ConnectionString);
    }
}
