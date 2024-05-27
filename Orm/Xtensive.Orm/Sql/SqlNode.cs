// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;

namespace Xtensive.Sql
{
  /// <summary>
  /// Represents any node in Sql expression tree.
  /// </summary>
  [Serializable]
  public abstract class SqlNode : ISqlNode
  {
    /// <summary>
    /// Gets the type of the node.
    /// </summary>
    /// <value>The type of the node.</value>
    public SqlNodeType NodeType { get; internal set; }

    object ICloneable.Clone() => Clone();

    internal abstract SqlNode Clone(SqlNodeCloneContext? context = null);

    internal SqlNode(SqlNodeType nodeType)
    {
      NodeType = nodeType;
    }

    public abstract void AcceptVisitor(ISqlVisitor visitor);
  }
}