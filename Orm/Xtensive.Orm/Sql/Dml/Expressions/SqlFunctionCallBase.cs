// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public abstract class SqlFunctionCallBase : SqlExpression
  {
    /// <summary>
    /// Gets the expressions.
    /// </summary>
    public IReadOnlyList<SqlExpression> Arguments { get; protected set; }

    internal SqlFunctionCallBase(SqlNodeType nodeType, IReadOnlyList<SqlExpression> arguments)
      : base(nodeType)
    {
      Arguments = arguments;
    }

    internal SqlFunctionCallBase(SqlNodeType nodeType, SqlExpression[] arguments)
      : base(nodeType)
    {
      Arguments = arguments ?? Array.Empty<SqlExpression>();
    }
  }
}