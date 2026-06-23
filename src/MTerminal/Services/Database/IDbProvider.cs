using System.Data.Common;

namespace MTerminal.Services.Database;

public interface IDbProvider
{
    DbConnection CreateConnection();
    SqlGuardProfile GuardProfile { get; }
}
