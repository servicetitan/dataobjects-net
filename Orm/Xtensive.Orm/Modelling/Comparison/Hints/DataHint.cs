// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.06.01

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xtensive.Core;


namespace Xtensive.Modelling.Comparison.Hints
{
  /// <summary>
  /// An abstract base class for all data hints.
  /// </summary>
  [Serializable]
  public abstract class DataHint : Hint
  {
    /// <summary>
    /// Gets the source table path.
    /// </summary>
    public string SourceTablePath { get; private set; }

    /// <summary>
    /// Gets the identities for data operation.
    /// </summary>
    public IReadOnlyList<IdentityPair> Identities { get; private set; }
    
    /// <inheritdoc/>
    public override List<HintTarget> GetTargets()
    {
      var targets = new List<HintTarget>(Identities.Count + 1);
      targets.Add(new HintTarget(ModelType.Source, SourceTablePath));
      foreach (var pair in Identities) {
        targets.Add(new HintTarget(ModelType.Source, pair.Source));
        if (!pair.IsIdentifiedByConstant)
          targets.Add(new HintTarget(ModelType.Source, pair.Target));
      }
      return targets;
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    protected DataHint(string sourceTablePath,  IReadOnlyList<IdentityPair> identities)
    {
      ArgumentException.ThrowIfNullOrEmpty(sourceTablePath);
      ArgumentNullException.ThrowIfNull(identities, "pairs");
      
      SourceTablePath = sourceTablePath;
      Identities = identities.AsSafeWrapper();
    }
  }
}
