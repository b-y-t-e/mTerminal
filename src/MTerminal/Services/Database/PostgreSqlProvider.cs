using System.Data.Common;
using Npgsql;

namespace MTerminal.Services.Database;

public sealed class PostgreSqlProvider : IDbProvider
{
    private readonly string _connectionString;

    public PostgreSqlProvider(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false };
        _connectionString = builder.ConnectionString;
    }

    public DbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public SqlGuardProfile GuardProfile => SqlGuardProfile.PostgreSql();
}
