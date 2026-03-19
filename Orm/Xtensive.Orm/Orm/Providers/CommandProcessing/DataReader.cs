// Copyright (C) 2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Core;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// <see cref="DataReader"/> behaves as synchronous or asynchronous enumerator
  /// over either regular <see cref="IEnumerable{T}"/> of <see cref="Tuple"/>s
  /// or over the running <see cref="Command"/> instance.
  /// </summary>
  public abstract class DataReader : IEnumerator<Tuple>, IAsyncEnumerator<Tuple>
  {
    public abstract bool MoveNext();
    public abstract ValueTask<bool> MoveNextAsync();
    public abstract void Reset();
    public abstract void Dispose();
    public abstract ValueTask DisposeAsync();

    public abstract Tuple Current { get; }
    object IEnumerator.Current => Current;

    public abstract bool IsInMemory { get; }
  }

  internal sealed class InMemoryDataReader(IEnumerable<Tuple> tuples) : DataReader
  {
    private readonly IEnumerator<Tuple> source = tuples.GetEnumerator();

    public override bool IsInMemory => true;

    public override Tuple Current => source.Current;

    public override bool MoveNext() => source.MoveNext();

    public override void Reset() => source.Reset();

    public override void Dispose() => source.Dispose();

    public override async ValueTask DisposeAsync() => await ((IAsyncEnumerator<Tuple>) source).DisposeAsync().ConfigureAwaitFalse();

    public override ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(MoveNext());
  }

  internal sealed class CommandDataReader(Command command, DbDataReaderAccessor accessor, CancellationToken token) : DataReader
  {
    /// <summary>
    /// Indicates current <see cref="DataReader"/> is built
    /// over <see cref="IEnumerable{T}"/> of <see cref="Tuple"/>s data source.
    /// </summary>
    public override bool IsInMemory => false;

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public override Tuple Current => command.ReadTupleWith(accessor);

    /// <inheritdoc/>
    public override bool MoveNext()
    {
      if (command.NextRow()) {
        return true;
      }

      // We don't need the command anymore because all records are processed to the moment.
      command.Dispose();
      return false;
    }

    /// <inheritdoc/>
    public override async ValueTask<bool> MoveNextAsync()
    {
      if (await command.NextRowAsync(token).ConfigureAwaitFalse()) {
        return true;
      }

      // We don't need the command anymore because all records are processed to the moment.
      await command.DisposeAsync().ConfigureAwaitFalse();
      return false;
    }

    /// <inheritdoc/>
    public override void Reset() => throw new NotSupportedException("Multiple enumeration is not supported.");

    /// <inheritdoc/>
    public override void Dispose() => command.Dispose();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
      if (source is Command command) {
        await command.DisposeAsync().ConfigureAwait(false);
      }
      else {
        if (source is IAsyncEnumerator<Tuple> asyncSource) {
          // true async enumerable source
          await asyncSource.DisposeAsync().ConfigureAwait(false);
        }
        else {
          // preloaded collection of elements,
          // like in case of delayed query which has already been read from database
          // or greedy enumeration
          ((IEnumerator<Tuple>) source).Dispose();
        }
      }
    }

    /// <summary>
    /// Creates <see cref="DataReader"/> wrapping enumerable collection of <see cref="Tuple"/>s.
    /// </summary>
    /// <param name="tuples">Collection of <see cref="Tuple"/>s to read from.</param>
    public DataReader(IEnumerable<Tuple> tuples)
    {
      source = tuples.GetEnumerator();
      accessor = null;
      token = CancellationToken.None;
    }

    /// <summary>
    /// Creates <see cref="DataReader"/> wrapping active <see cref="Command"/> instance.
    /// </summary>
    /// <param name="command"><see cref="Command"/> instance to read data from.</param>
    /// <param name="accessor"><see cref="DbDataReaderAccessor"/> instance
    /// transforming raw database records to <see cref="Tuple"/>s.</param>
    /// <param name="token"><see cref="CancellationToken"/> to terminate operation if necessary.</param>
    public DataReader(Command command, DbDataReaderAccessor accessor, CancellationToken token)
    {
      source = command;
      this.accessor = accessor;
      this.token = token;
    }
  }
}