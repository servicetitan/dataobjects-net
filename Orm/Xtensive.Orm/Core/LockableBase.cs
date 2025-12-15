// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2007.11.22

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Xtensive.Core;

/// <summary>
/// Base class for <see cref="ILockable"/> implementors.
/// </summary>
[Serializable]
public abstract class LockableBase(bool isLocked = false) : ILockable
{
  /// <inheritdoc/>
  public bool IsLocked { [DebuggerStepThrough] get; private set; } = isLocked;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static void ThrowInstanceIsLockedException() => throw new InstanceIsLockedException(Strings.ExInstanceIsLocked);

  /// <summary>
  /// Ensures the object is not locked (see <see cref="ILockable.Lock()"/>) yet.
  /// </summary>
  /// <exception cref="InstanceIsLockedException">The instance is locked.</exception>
  public void EnsureNotLocked()
  {
    if (IsLocked) {
      ThrowInstanceIsLockedException();
    }
  }

  /// <inheritdoc/>
  public void Lock() => Lock(true);

  /// <inheritdoc/>
  public virtual void Lock(bool recursive) => IsLocked = true;
}
