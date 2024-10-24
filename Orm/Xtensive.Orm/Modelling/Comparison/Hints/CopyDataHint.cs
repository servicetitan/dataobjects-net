// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.05.30

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xtensive.Core;


namespace Xtensive.Modelling.Comparison.Hints
{
  /// <summary>
  /// Hint for copy data operation.
  /// </summary>
  [Serializable]
  public sealed class CopyDataHint : DataHint
  {
    /// <summary>
    /// Gets copied columns. The first value is source column path, 
    /// the second value is destination column path.
    /// </summary>
    public IReadOnlyList<Pair<string>> CopiedColumns { get; private set; }
    
    /// <inheritdoc/>
    public override List<HintTarget> GetTargets()
    {
      var targets = new List<HintTarget>(CopiedColumns.Count * 2 + Identities.Count + 1);
      targets.Add(new HintTarget(ModelType.Source, SourceTablePath));
      foreach (var pair in CopiedColumns) {
        targets.Add(new HintTarget(ModelType.Source, pair.First));
        targets.Add(new HintTarget(ModelType.Target, pair.Second));
      }
      foreach (var pair in Identities) {
        targets.Add(new HintTarget(ModelType.Source, pair.Source));
        if (!pair.IsIdentifiedByConstant)
          targets.Add(new HintTarget(ModelType.Target, pair.Target));
      }
      return targets;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      return string.Format(
        "Copy from '{0}' to '{1}' where ({2})",
        string.Join(", ", CopiedColumns.Select(pair => pair.First)
          .ToArray()),
        string.Join(", ", CopiedColumns.Select(pair => pair.Second)
          .ToArray()),
        string.Join(" and ", Identities.Select(pair => pair.ToString())
          .ToArray()));
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    public CopyDataHint(string sourceTablePath,  IReadOnlyList<IdentityPair> identities,
      IReadOnlyList<Pair<string>> copiedColumns)
      : base(sourceTablePath, identities)
    {
      ArgumentNullException.ThrowIfNull(copiedColumns);
      CopiedColumns = copiedColumns.AsSafeWrapper();
    }
  }
}