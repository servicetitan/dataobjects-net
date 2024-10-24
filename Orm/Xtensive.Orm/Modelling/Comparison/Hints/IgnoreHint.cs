// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.05.28

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xtensive.Core;


namespace Xtensive.Modelling.Comparison.Hints
{
  /// <summary>
  /// Ignore node hint. 
  /// Add possibilities to ignore specified node in comparison.
  /// </summary>
  [Serializable]
  public class IgnoreHint : Hint
  {
    /// <summary>
    /// Gets ignored node path.
    /// </summary>
    public string Path { get; private set; }

    /// <inheritdoc/>
    public override List<HintTarget> GetTargets() =>
      new() { new HintTarget(ModelType.Source, Path) };

    /// <inheritdoc/>
    public override string ToString()
    {
      return $"Ignore '{Path}'";
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="path">The ignored node path.</param>
    public IgnoreHint(string path)
    {
      ArgumentException.ThrowIfNullOrEmpty(path);
      Path = path;
    }
  }
}
