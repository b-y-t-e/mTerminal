using System.Data.Common;

namespace mTiles.Services.Database;

public interface IDbProvider
{
    DbConnection CreateConnection();
    SqlGuardProfile GuardProfile { get; }
}
