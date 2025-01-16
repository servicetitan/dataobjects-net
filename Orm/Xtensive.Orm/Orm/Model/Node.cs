// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.07.25

using System;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Reflection;

namespace Xtensive.Orm.Model
{
  /// <summary>
  ///An abstract base class for model node.
  /// </summary>
  [Serializable]
  public abstract class Node : LockableBase
  {
    private string name;

    /// <summary>
    /// Gets the name of this instance.
    /// </summary>
    public string Name
    {
      get => name;
      set {
        EnsureNotLocked();
        if (name is not null)
          throw new InvalidOperationException("The node Name is locked.");
        ValidateName(value);
        name = value;
      }
    }

    /// <summary>
    /// Performs additional custom processes before setting new name to this instance.
    /// </summary>
    /// <param name="newName">The new name of this instance.</param>
    protected virtual void ValidateName(string newName)
    {
    }

    /// <summary>
    /// Updates the internal state of this instance.
    /// </summary>
    public virtual void UpdateState()
    {
      EnsureNotLocked();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      var type = GetType();

      return string.Format(Strings.NodeFormat,
        name ?? Strings.UnnamedNodeDisplayName,
        type.IsGenericType ? type.GetShortName() : type.Name);
    }


    // Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    protected Node(string name)
    {
      ArgumentException.ThrowIfNullOrEmpty(name);
      Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    protected Node()
    {
    }
  }
}
