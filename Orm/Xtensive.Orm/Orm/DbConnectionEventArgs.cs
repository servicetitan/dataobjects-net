using System.Data.Common;

namespace Xtensive.Orm
{
  /// <summary>
  /// Event args for <see cref="SessionEventAccessor.DbConnectionOpened"/>.
  /// </summary>
  public readonly struct DbConnectionEventArgs
  {
    /// <summary>
    /// Gets the connection that transitioned from closed to open.
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="connection">The connection that transitioned from closed to open.</param>
    public DbConnectionEventArgs(DbConnection connection)
    {
      Connection = connection;
    }
  }
}
