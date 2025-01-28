// Copyright (C) 2007-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2007.12.29

namespace Xtensive.Core
{
  /// <summary>
  /// Ensures all the <see cref="IDisposable"/> objects added to it are disposed
  /// on disposal of <see cref="DisposableSet"/> instance.
  /// </summary>
  /// <remarks>
  /// <note>
  /// <see cref="IDisposable.Dispose"/> methods are invoked in backward order.
  /// </note>
  /// </remarks>
  internal sealed class DisposableSet() : List<IDisposable>, IDisposable, IAsyncDisposable
  {
    private HashSet<IDisposable> set;

    /// <summary>
    /// Adds an <see cref="IDisposable"/> object to the set.
    /// </summary>
    /// <param name="disposable">The object to add.</param>
    /// <returns><see langword="True"/>, if object is successfully added;
    /// otherwise, <see langword="false"/>.</returns>
    public new bool Add(IDisposable disposable)
    {
      if (disposable==null)
        return false;
      EnsureInitialized();
      if (set.Add(disposable)) {
        base.Add(disposable);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Clears this instance by discarding all registered objects.
    /// <see cref="IDisposable.Dispose"/> methods are not called.
    /// </summary>
    public new void Clear()
    {
      set = null;
      base.Clear();
    }

    /// <summary>
    /// Joins the <see cref="DisposableSet"/> and <see cref="IDisposable"/>.
    /// </summary>
    /// <param name="first">The first disposable to join.</param>
    /// <param name="second">The second disposable to join.</param>
    /// <returns>New <see cref="JoiningDisposable"/> that will
    /// dispose both of them on its disposal</returns>
    public static JoiningDisposable operator &(DisposableSet first, IDisposable second)
    {
      return new JoiningDisposable(first, second);
    }

    private void EnsureInitialized()
    {
      set ??= new();
    }

    /// <summary>
    /// Releases resources associated with this instance.
    /// </summary>
    void IDisposable.Dispose()
    {
      try {
        if (Count == 0) {
          return;
        }

        using (var aggregator = new ExceptionAggregator()) {
          for (var i = Count - 1; i >= 0; i--) {
            aggregator.Execute(d => d.Dispose(), this[i]);
          }

          aggregator.Complete();
        }
      }
      finally {
        Clear();
      }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
      try {
        if (Count == 0) {
          return;
        }

        using (var aggregator = new ExceptionAggregator()) {
          for (var i = Count - 1; i >= 0; i--) {
            var disposable = this[i];
            if (disposable is IAsyncDisposable asyncDisposable) {
              await aggregator.ExecuteAsync(d => d.DisposeAsync(), asyncDisposable).ConfigureAwaitFalse();
            }
            else {
              aggregator.Execute(d => d.Dispose(), disposable);
            }
          }

          aggregator.Complete();
        }
      }
      finally {
        Clear();
      }
    }
  }
}
