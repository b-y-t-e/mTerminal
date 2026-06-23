using System.Data.Common;

namespace mTerminal.Services.Database;

public interface IDbProvider
{
    DbConnection CreateConnection();
    SqlGuardProfile GuardProfile { get; }
}
