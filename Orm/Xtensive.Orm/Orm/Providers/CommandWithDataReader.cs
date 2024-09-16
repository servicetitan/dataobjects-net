// Copyright (C) 2012-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.03.01

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xtensive.Core;

namespace Xtensive.Orm.Providers
{
  public readonly record struct CommandWithDataReader : IDisposable, IAsyncDisposable
  {
    public DbCommand Command { get; }
    public DbDataReader Reader { get; }

    public void Dispose()
    {
      // Dispose the reader first, at least firebird provider requires it
      Reader.Dispose();
      Command.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
      // Dispose the reader first, at least firebird provider requires it
      await Reader.DisposeAsync().ConfigureAwaitFalse();
      await Command.DisposeAsync().ConfigureAwaitFalse();
    }

    // Constructors

    internal CommandWithDataReader(DbCommand command, DbDataReader reader)
    {
      ArgumentNullException.ThrowIfNull(command);
      ArgumentNullException.ThrowIfNull(reader);
      Command = command;
      Reader = reader;
    }
  }
}
