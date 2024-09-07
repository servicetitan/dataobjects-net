// Copyright (C) 2019-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2019.07.12

using System;
using System.Collections.Concurrent;
using Xtensive.Orm.Providers;

namespace Xtensive.Orm
{
  internal struct CommandProcessorContextProvider()
  {
    private readonly ConcurrentDictionary<CommandProcessorContext, CommandProcessorContext> providedContexts = new();

    /// <inheritdoc/>
    public int AliveContextCount => providedContexts.Count;

    /// <inheritdoc/>
    public CommandProcessorContext ProvideContext() => ProvideContext(false);

    /// <inheritdoc/>
    public CommandProcessorContext ProvideContext(bool allowPartialExecution)
    {
      var context = new CommandProcessorContext(null, allowPartialExecution);
      providedContexts.AddOrUpdate(
        context, (CommandProcessorContext) null, (processorContext, commandProcessorContext) => null);
      context.Disposed += RemoveDisposedContext;
      return context;
    }

    private void RemoveDisposedContext(object sender, EventArgs args)
    {
      var context = (CommandProcessorContext) sender;
      providedContexts.TryRemove(context, out _);
      context.Disposed -= RemoveDisposedContext;
    }
  }
}
