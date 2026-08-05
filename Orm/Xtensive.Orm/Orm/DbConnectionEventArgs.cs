using System.Data.Common;

namespace Xtensive.Orm
{
  /// <summary>
  /// Event args carrying a <see cref="DbConnection"/>, used by <see cref="SessionEventAccessor.DbConnectionOpened"/>
  /// and <see cref="SessionEventAccessor.RawConnectionAccessed"/>.
  /// </summary>
  /// <param name="Connection">The connection the event concerns.</param>
  public readonly record struct DbConnectionEventArgs(DbConnection Connection);
}
