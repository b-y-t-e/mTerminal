using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace mTerminal.Services.Database;

public sealed class SqlServerProvider : IDbProvider
{
    private readonly string _connectionString;

    public SqlServerProvider(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { Pooling = false };
        _connectionString = builder.ConnectionString;
    }

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);

    public SqlGuardProfile GuardProfile => SqlGuardProfile.SqlServer();
}
