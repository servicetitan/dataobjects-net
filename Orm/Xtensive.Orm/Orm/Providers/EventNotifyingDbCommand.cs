using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Core;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Wraps a raw <see cref="DbCommand"/> handed out by <see cref="IDirectSqlService.CreateCommand"/>,
  /// raising the same <see cref="SessionEventAccessor.DbCommandExecuting"/>/
  /// <see cref="SessionEventAccessor.DbCommandExecuted"/> events around execution that the ORM's own
  /// commands raise, without translating any exception the inner command throws.
  /// </summary>
  internal sealed class EventNotifyingDbCommand : DbCommand
  {
    private readonly Session session;
    private readonly DbCommand innerCommand;

    public override string CommandText
    {
      get => innerCommand.CommandText;
      set => innerCommand.CommandText = value;
    }

    public override int CommandTimeout
    {
      get => innerCommand.CommandTimeout;
      set => innerCommand.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
      get => innerCommand.CommandType;
      set => innerCommand.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
      get => innerCommand.UpdatedRowSource;
      set => innerCommand.UpdatedRowSource = value;
    }

    public override bool DesignTimeVisible
    {
      get => innerCommand.DesignTimeVisible;
      set => innerCommand.DesignTimeVisible = value;
    }

    protected override DbConnection DbConnection
    {
      get => innerCommand.Connection;
      set => innerCommand.Connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => innerCommand.Parameters;

    protected override DbTransaction DbTransaction
    {
      get => innerCommand.Transaction;
      set => innerCommand.Transaction = value;
    }

    public override void Cancel() => innerCommand.Cancel();

    protected override DbParameter CreateDbParameter() => innerCommand.CreateParameter();

    public override void Prepare() => innerCommand.Prepare();

    public override Task PrepareAsync(CancellationToken cancellationToken = default) =>
      innerCommand.PrepareAsync(cancellationToken);

    public override int ExecuteNonQuery()
    {
      session.Events.NotifyDbCommandExecuting(this);
      try {
        var result = innerCommand.ExecuteNonQuery();
        session.Events.NotifyDbCommandExecuted(this);
        return result;
      }
      catch (Exception exception) {
        session.Events.NotifyDbCommandExecuted(this, exception);
        throw;
      }
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
      session.Events.NotifyDbCommandExecuting(this);
      try {
        var result = await innerCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwaitFalse();
        session.Events.NotifyDbCommandExecuted(this);
        return result;
      }
      catch (Exception exception) {
        session.Events.NotifyDbCommandExecuted(this, exception);
        throw;
      }
    }

    public override object ExecuteScalar()
    {
      session.Events.NotifyDbCommandExecuting(this);
      try {
        var result = innerCommand.ExecuteScalar();
        session.Events.NotifyDbCommandExecuted(this);
        return result;
      }
      catch (Exception exception) {
        session.Events.NotifyDbCommandExecuted(this, exception);
        throw;
      }
    }

    public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
      session.Events.NotifyDbCommandExecuting(this);
      try {
        var result = await innerCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwaitFalse();
        session.Events.NotifyDbCommandExecuted(this);
        return result;
      }
      catch (Exception exception) {
        session.Events.NotifyDbCommandExecuted(this, exception);
        throw;
      }
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
      session.Events.NotifyDbCommandExecuting(this);
      try {
        var result = innerCommand.ExecuteReader(behavior);
        session.Events.NotifyDbCommandExecuted(this);
        return result;
      }
      catch (Exception exception) {
        session.Events.NotifyDbCommandExecuted(this, exception);
        throw;
      }
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
      CommandBehavior behavior, CancellationToken cancellationToken)
    {
      session.Events.NotifyDbCommandExecuting(this);
      try {
        var result = await innerCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwaitFalse();
        session.Events.NotifyDbCommandExecuted(this);
        return result;
      }
      catch (Exception exception) {
        session.Events.NotifyDbCommandExecuted(this, exception);
        throw;
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing) {
        innerCommand.Dispose();
      }
      base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync() => innerCommand.DisposeAsync();

    public EventNotifyingDbCommand(Session session, DbCommand innerCommand)
    {
      this.session = session;
      this.innerCommand = innerCommand;
    }
  }
}
