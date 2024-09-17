// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.04.22

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Modelling.Actions
{
  /// <summary>
  /// A grouping node action.
  /// </summary>
  [Serializable]
  public class GroupingNodeAction : NodeAction
  {
    private string comment;
    private IReadOnlyList<NodeAction> actions = new List<NodeAction>();

    /// <summary>
    /// Gets or sets the comment.
    /// </summary>
    public string Comment {
      get { return comment; }
      set {
        EnsureNotLocked();
        comment = value;
      }
    }

    /// <summary>
    /// Gets the list of nested actions.
    /// </summary>
    public IReadOnlyList<NodeAction> Actions => actions;

    /// <summary>
    /// Adds the specified action to the <see cref="Actions"/> sequence.
    /// </summary>
    /// <param name="action">The action to add.</param>
    public void Add(NodeAction action)
    {
      ArgumentNullException.ThrowIfNull(action, "action");
      EnsureNotLocked();
      // Only locked actions can be added
      var ca = action as PropertyChangeAction;
      if (ca!=null && actions.Count!=0) {
        // Let's try to join two change actions
        var lastIndex = actions.Count - 1;
        var last = actions[lastIndex] as PropertyChangeAction;
        if (last!=null && ca.Path==last.Path) {
          foreach (var pair in last.Properties) {
            _ = ca.Properties.TryAdd(pair.Key, pair.Value);
          }
          ((List<NodeAction>)actions).RemoveAt(lastIndex);
        }
      }
      ((List<NodeAction>)actions).Add(action);
    }

    /// <summary>
    /// Flattens all the <see cref="GroupingNodeAction"/>s from <see cref="Actions"/> sequence.
    /// </summary>
    /// <returns>Flattened sequence of nested actions.</returns>
    public IEnumerable<NodeAction> Flatten()
    {
      foreach (var action in actions) {
        var gna = action as GroupingNodeAction;
        if (gna!=null)
          foreach (var nestedAction in gna.Flatten())
            yield return nestedAction;
        else
          yield return action;
      }
    }

    /// <inheritdoc/>
    protected override void PerformExecute(IModel model, IPathNode item)
    {
      foreach (var action in actions)
        action.Execute(model);
    }

    /// <inheritdoc/>
    protected override void GetParameters(List<Pair<string>> parameters)
    {
      base.GetParameters(parameters);
      if (!comment.IsNullOrEmpty())
        parameters.Add(new Pair<string>("Comment", comment));
    }

    /// <inheritdoc/>
    protected override IEnumerable<NodeAction> GetNestedActions()
    {
      return base.GetNestedActions().Concat(actions);
    }

    /// <inheritdoc/>
    public override void Lock(bool recursive)
    {
      base.Lock(recursive);
      if (recursive)
        foreach (var action in actions)
          action.Lock(true);
      actions = actions.ToArray().AsSafeWrapper();
    }
  }
}