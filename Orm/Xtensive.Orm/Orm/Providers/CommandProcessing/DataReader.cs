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
  public interface DataReader : IEnumerator<Tuple>, IAsyncEnumerator<Tuple>
  {
    bool IsInMemory { get; }
    Tuple Current { get; }
  }

  internal sealed class InMemoryDataReader(IEnumerable<Tuple> tuples) : DataReader
  {
    private readonly IEnumerator<Tuple> source = tuples.GetEnumerator();

    public bool IsInMemory => true;

    public Tuple Current => source.Current;

    object IEnumerator.Current => Current;

    public bool MoveNext() => source.MoveNext();

    public void Reset() => source.Reset();

    public void Dispose() => source.Dispose();

    public async ValueTask DisposeAsync() => await ((IAsyncEnumerator<Tuple>) source).DisposeAsync().ConfigureAwaitFalse();

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(MoveNext());
  }

  internal sealed class CommandDataReader(Command command, DbDataReaderAccessor accessor, CancellationToken token) : DataReader
  {
    /// <summary>
    /// Indicates current <see cref="DataReader"/> is built
    /// over <see cref="IEnumerable{T}"/> of <see cref="Tuple"/>s data source.
    /// </summary>
    public bool IsInMemory => false;

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public Tuple Current => command.ReadTupleWith(accessor);

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
      if (command.NextRow()) {
        return true;
      }

      // We don't need the command anymore because all records are processed to the moment.
      command.Dispose();
      return false;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
      if (await command.NextRowAsync(token).ConfigureAwaitFalse()) {
        return true;
      }

      // We don't need the command anymore because all records are processed to the moment.
      await command.DisposeAsync().ConfigureAwaitFalse();
      return false;
    }

    /// <inheritdoc/>
    public void Reset() => throw new NotSupportedException("Multiple enumeration is not supported.");

    /// <inheritdoc/>
    public void Dispose() => command.Dispose();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await command.DisposeAsync().ConfigureAwaitFalse();
  }
}
