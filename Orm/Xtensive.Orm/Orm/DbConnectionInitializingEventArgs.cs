// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;

namespace Xtensive.Orm
{
  /// <summary>
  /// Event args for <see cref="SessionEventAccessor.DbConnectionInitializing"/>, raised before a connection's
  /// initialization SQL runs. Subscribers may append SQL to the initialization batch, which then executes as a
  /// single batch when the connection opens. The connection is not yet open and the seeded SQL (for example
  /// <c>USE [db]</c>) has not yet run, so a subscriber may only append to the batch, not issue its own commands.
  /// </summary>
  public class DbConnectionInitializingEventArgs
  {
    /// <summary>
    /// Gets the session whose connection is being initialized.
    /// </summary>
    public Session Session { get; }

    /// <summary>
    /// Gets the initialization SQL that will run when the connection opens, including any appended by
    /// subscribers.
    /// </summary>
    public string InitializationScript { get; private set; }

    /// <summary>
    /// Appends <paramref name="sql"/> to the initialization batch, after any SQL already present.
    /// </summary>
    public void AppendInitializationSql(string sql)
    {
      if (string.IsNullOrEmpty(sql)) {
        return;
      }
      if (string.IsNullOrEmpty(InitializationScript)) {
        InitializationScript = sql;
        return;
      }
      InitializationScript = $"{InitializationScript.TrimEnd().TrimEnd(';')}; {sql}";
    }

    // Constructors

    internal DbConnectionInitializingEventArgs(Session session, string initializationScript)
    {
      Session = session;
      InitializationScript = initializationScript;
    }
  }
}
