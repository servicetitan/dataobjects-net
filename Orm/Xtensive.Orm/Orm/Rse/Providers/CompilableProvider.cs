// Copyright (C) 2003-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2008.07.03

using System;
using System.Collections.Generic;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Abstract base class for any query provider,
  /// that requires storage-specific compilation before in can be executed.
  /// </summary>
  [Serializable]
  public abstract class CompilableProvider(ProviderType type, params Provider[] sources) : Provider(type, sources)
  {
    internal abstract Provider Visit(ProviderVisitor visitor);

    // Constructors

    protected CompilableProvider(ProviderType type)
      : this(type, [])
    {
    }
  }
}