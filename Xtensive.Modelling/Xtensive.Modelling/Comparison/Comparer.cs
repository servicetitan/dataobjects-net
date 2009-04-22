// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.04.07

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Disposing;
using Xtensive.Modelling.Comparison.Hints;
using Xtensive.Modelling.Resources;

namespace Xtensive.Modelling.Comparison
{
  /// <summary>
  /// Abstract base class for <see cref="IComparer"/> implementation.
  /// </summary>
  public class Comparer : IComparer
  {
    [ThreadStatic]
    private static Comparer current;

    #region Properties: Current, Context, Source, Target, Hints

    /// <summary>
    /// Gets the current comparer.
    /// </summary>
    public static Comparer Current {
      get { return current; }
    }

    /// <summary>
    /// Gets the current comparison context.
    /// </summary>
    protected internal ComparisonContext Context { get; internal set; }

    /// <summary>
    /// Gets the source model.
    /// </summary>
    protected IModel Source { get; private set; }

    /// <summary>
    /// Gets the target model.
    /// </summary>
    protected IModel Target { get; private set; }

    /// <summary>
    /// Gets the comparison hints.
    /// </summary>
    protected HintSet Hints { get; private set; }

    /// <summary>
    /// Gets the dictionary of all already found differences of objects
    /// from <see cref="Source"/> and <see cref="Target"/> models.
    /// Maps objects from both <see cref="Source"/> and <see cref="Target"/>
    /// to their <see cref="Difference"/>.
    /// </summary>
    protected Dictionary<object, Difference> Results { get; private set; }

    /// <summary>
    /// Gets the current comparison stage.
    /// </summary>
    protected ComparisonStage Stage { get; private set; }

    #endregion

    /// <inheritdoc/>
    public Difference Compare(IModel source, IModel target)
    {
      return Compare(source, target, null);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException"><c>hints.SourceModel</c> or <c>hints.TargetModel</c>
    /// is out of range.</exception>
    public Difference Compare(IModel source, IModel target, HintSet hints)
    {
      Source = source;
      Target = target;
      Hints = hints ?? new HintSet(Source, Target);
      if (Hints.SourceModel!=Source)
        throw new ArgumentOutOfRangeException("hints.SourceModel");
      if (Hints.TargetModel!=Target)
        throw new ArgumentOutOfRangeException("hints.TargetModel");
      var previous = current;
      current = this;
      Results = new Dictionary<object, Difference>();
      try {
        Stage = ComparisonStage.BaseComparison;
        Visit(Source, Target);
        Stage = ComparisonStage.ReferenceComparison;
        return Visit(Source, Target);
      }
      finally {
        current = previous;
        Results = null;
      }
    }

    /// <summary>
    /// Visitor dispatcher.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="target">The target.</param>
    /// <returns>Difference between <paramref name="source"/> 
    /// and <paramref name="target"/> objects.
    /// <see langword="null" />, if they're equal.</returns>
    protected Difference Visit(object source, object target)
    {
      return Visit(GetCommonBase(source, target), source, target);
    }

    /// <summary>
    /// Visitor dispatcher.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="source">The source.</param>
    /// <param name="target">The target.</param>
    /// <returns>Difference between <paramref name="source"/> 
    /// and <paramref name="target"/> objects.
    /// <see langword="null" />, if they're equal.</returns>
    protected virtual Difference Visit(Type type, object source, object target)
    {
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      if (typeof (Node).IsAssignableFrom(type))
        return VisitNode((Node) source, (Node) target);
      if (typeof(NodeCollection).IsAssignableFrom(type))
        return VisitNodeCollection((NodeCollection) source, (NodeCollection) target);
      return VisitObject(source, target);
    }

    /// <summary>
    /// Visits specified <see cref="Node"/> objects.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="target">The target.</param>
    /// <returns>
    /// Difference between <paramref name="source"/>
    /// and <paramref name="target"/> objects.
    /// <see langword="null"/>, if they're equal.
    /// </returns>
    /// <exception cref="InvalidOperationException">Both source and target are <see langword="null" />.</exception>
    /// <exception cref="NullReferenceException">Current difference is not <see cref="NodeDifference"/>.</exception>
    protected virtual Difference VisitNode(Node source, Node target)
    {
      using (TryActivate(source, target, (s,t) => new NodeDifference(s,t))) {
        var context = Context;
        var difference = (NodeDifference) context.Difference;
        if (difference==null)
          throw new NullReferenceException();
        var any = source ?? target;
        if (any==null)
          throw Exceptions.InternalError(Strings.ExBothSourceAndTargetAreNull, Log.Instance);
        
        bool isNewDifference = TryRegisterDifference(source, target, difference);
        var mi = difference.MovementInfo;

        if (isNewDifference) {
          // Filling MovementInfo
          if (source==null) {
            // => target!=null
            mi |= MovementInfo.Created;
            if (difference.Parent!=null) {
              var parentSource = difference.Parent.Source as Node;
              if (parentSource!=null && context.Property!=null &&
                parentSource.GetProperty(context.Property.Name)!=null)
                mi |= MovementInfo.Removed; // = recreated
            }
          }
          else if (target==null)
            mi |= MovementInfo.Removed;
          else {
            // both source!=null && target!=null
            if (!(source is IUnnamedNode) && source.Name!=target.Name && GetTargetPath(source)==target.Path)
              mi |= MovementInfo.NameChanged;
            var collection = target.Nesting.PropertyValue as NodeCollection;
            if (source.Index!=target.Index &&
              !(collection!=null && (collection is IUnorderedNodeCollection))) // TODO: Fix this!
              mi |= MovementInfo.IndexChanged;
            var pdc = context.GetParentDifferenceContext<NodeDifference>();
            if (pdc!=null) {
              var parentDifference = (NodeDifference) pdc.Difference;
              if (source.Parent!=parentDifference.Source || target.Parent!=parentDifference.Target)
                mi |= MovementInfo.ParentChanged;
              var parentMi = parentDifference.MovementInfo;
              if ((parentMi & MovementInfo.Relocated)!=0)
                mi |= MovementInfo.ParentRelocated;
            }
          }
          difference.MovementInfo = mi;
        }

        // Comparing properties
        difference.PropertyChanges.Clear();
        if ((mi & MovementInfo.Removed)==0 || (mi & MovementInfo.Created)!=0) {
          foreach (var pair in any.PropertyAccessors) {
            var accessor = pair.Value;
            if (accessor.IgnoreInComparison)
              continue;

            var property = accessor.PropertyInfo;
            using (CreateContext().Activate()) {
              Context.Property = property;
              object newSource = (source==null || !accessor.HasGetter)
                ? accessor.Default : accessor.Getter(source);
              object newTarget = (target==null || !accessor.HasGetter)
                ? accessor.Default : accessor.Getter(target);

              var newAny = newSource ?? newTarget;
              if (newAny==null)
                continue; // Both are null

              if (IsReference(newSource, newTarget)) {
                if (Stage==ComparisonStage.ReferenceComparison && 
                    newSource!=null && newTarget!=null) { // Otherwise value is definitely changed
                  Difference newDifference = null;
                  if (!Results.TryGetValue(newTarget, out newDifference))
                    throw new InvalidOperationException(string.Format(
                      Strings.ExNodeXMustBeProcessedBeforeBeingComparedAsReferenceValueOfYZ,
                      newTarget, target, property));
                  if (!IsRelocated(newDifference))
                    continue;
                }
                difference.PropertyChanges.Add(property.Name, 
                  new ValueDifference(newSource, newTarget));
              }
              else {
                var newDifference = Visit(newSource, newTarget);
                if (newDifference!=null)
                  difference.PropertyChanges.Add(property.Name, newDifference);
              }
            }
          }
        }

        return difference.HasChanges ? difference : null;
      }
    }

    protected virtual bool IsReference(object source, object target)
    {
      var difference = Context.Difference;
      var any = (target ?? source) as Node;
      if (any!=null)
        return any.Parent!=difference.Source && any.Parent!=difference.Target;
      return false;
    }

    protected virtual bool IsRelocated(Difference difference)
    {
      var nodeDifference = difference as NodeDifference;
      if (nodeDifference==null)
        return false;
      if ((nodeDifference.MovementInfo & MovementInfo.Relocated)==0)
        return false;
      var diffs = 
        EnumerableUtils.Unfold(difference, d => d.Parent).Reverse().ToList();
      var currentDiffs = 
        EnumerableUtils.Unfold(Context.Difference, d => d.Parent).Reverse().ToList();
      var commonDiffs = diffs.Zip(currentDiffs).Where(p => p.First==p.Second).Select(p => p.First).ToList();
      var newDiffs = diffs.Except(commonDiffs).ToList();
      var query =
        from diff in newDiffs
        let nodeDiff = diff as NodeDifference
        where nodeDiff!=null && (nodeDiff.MovementInfo & MovementInfo.Changed)!=0
        select nodeDiff;
      query = query.ToList();
      return query.Any();
    }

    /// <summary>
    /// Visits specified <see cref="NodeCollection"/> objects.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="target">The target.</param>
    /// <returns>Difference between <paramref name="source"/> 
    /// and <paramref name="target"/> objects.
    /// <see langword="null" />, if they're equal.</returns>
    /// <exception cref="InvalidOperationException">Both source and target are <see langword="null" />.</exception>
    /// <exception cref="NullReferenceException">Current difference is not <see cref="NodeCollectionDifference"/>.</exception>
    protected virtual Difference VisitNodeCollection(NodeCollection source, NodeCollection target)
    {
      using (TryActivate(source, target, (s,t) => new NodeCollectionDifference(s,t))) {
        var context = Context;
        var difference = (NodeCollectionDifference) context.Difference;
        if (difference==null)
          throw new NullReferenceException();
        context.Property = null;
        
        bool isNewDifference = TryRegisterDifference(source, target, difference);
        difference.ItemChanges.Clear();

        var src = ((ICountable) source) ?? new ReadOnlyList<Node>(new Node[] {});
        var tgt = ((ICountable) target) ?? new ReadOnlyList<Node>(new Node[] {});

        if (src.Count==0 && tgt.Count==0)
          return null;
        var someItems = src.Count!=0 ? src : tgt;
        var someItem = someItems.Cast<Node>().First();

        Func<Node, Pair<Node, object>> keyExtractor = 
          n => new Pair<Node, object>(n, GetNodeComparisonKey(n));

        var sourceKeyMap = new Dictionary<object, Node>();
        foreach (var pair in src.Cast<Node>().Select(keyExtractor))
          sourceKeyMap.Add(pair.Second, pair.First);

        var targetKeyMap = new Dictionary<object, Node>();
        foreach (var pair in tgt.Cast<Node>().Select(keyExtractor))
          targetKeyMap.Add(pair.Second, pair.First);

        var sourceKeys = src.Cast<Node>().Select(n => keyExtractor(n).Second);
        var targetKeys = tgt.Cast<Node>().Select(n => keyExtractor(n).Second);
        var commonKeys = sourceKeys.Intersect(targetKeys);

        var sequence =
          sourceKeys.Except(commonKeys)
            .Select(k => new {Index = sourceKeyMap[k].Index, Type = 0, 
              Source = sourceKeyMap[k], Target = (Node) null})
          .Concat(commonKeys
            .Select(k => new {Index = targetKeyMap[k].Index, Type = 1, 
              Source = sourceKeyMap[k], Target = targetKeyMap[k]}))
          .Concat(targetKeys.Except(commonKeys)
            .Select(k => new {Index = targetKeyMap[k].Index, Type = 2, 
              Source = (Node) null, Target = targetKeyMap[k]}))
          .OrderBy(i => i.Type!=0).ThenBy(i => i.Index).ThenBy(i => i.Type);

        foreach (var i in sequence) {
          var d = Visit(i.Source, i.Target);
          if (d!=null)
            difference.ItemChanges.Add((NodeDifference) d);
        }

        return (difference.ItemChanges.Count!=0) ? difference : null;
      }
    }

    /// <summary>
    /// Extracts the comparison key, that used to find associations 
    /// between old and new <see cref="NodeCollection"/> items.
    /// </summary>
    /// <param name="node">The node to get the comparison key for.</param>
    /// <returns>Comparison key for the specified node.</returns>
    protected virtual object GetNodeComparisonKey(Node node)
    {
      if (node is INodeReference) {
        var targetNode = ((INodeReference) node).Value;
        return targetNode==null ? null : GetTargetPath(targetNode);
      }
      else
        return GetTargetName(node);
    }

    /// <summary>
    /// Visits specified objects.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="target">The target.</param>
    /// <returns>Difference between <paramref name="source"/> 
    /// and <paramref name="target"/> objects.
    /// <see langword="null" />, if they're equal.</returns>
    protected virtual Difference VisitObject(object source, object target)
    {
      using (TryActivate(source, target, (s, t) => new ValueDifference(s, t)))
        return Equals(source, target) ? null : Context.Difference;
    }

    #region Helper methods

    /// <summary>
    /// Tries to create the new <see cref="ComparisonContext"/> 
    /// for the specified <paramref name="source"/>
    /// and <paramref name="target"/> objects and activate it.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="source"/> and <paramref name="target"/> objects.</typeparam>
    /// <param name="source">The source object to compare.</param>
    /// <param name="target">The target object to compare.</param>
    /// <param name="differenceGenerator">The difference generator.</param>
    /// <returns>A disposable object deactivating the context, if it was activated;
    /// otherwise, <see langword="null" />.</returns>
    protected IDisposable TryActivate<T>(T source, T target, Func<T, T, Difference> differenceGenerator)
      where T: class
    {
      var context = Context;
      if (context!=null) {
        var difference = context.Difference;
        if (difference!=null && difference.Source==source && difference.Target==target)
          return null;
      }
      context = CreateContext();
      var result = context.Activate();
      try {
        Context.Difference = differenceGenerator.Invoke(source, target);
        if (!(Context.Difference is ValueDifference)) {
          Difference difference = null;
          if (Results.TryGetValue(target ?? source, out difference))
            Context.Difference = difference;
        }
        return result;
      }
      catch {
        result.DisposeSafely();
        throw;
      }
    }

    /// <summary>
    /// Creates new comparison context.
    /// </summary>
    /// <returns>Newly created <see cref="ComparisonContext"/> instance.</returns>
    protected virtual ComparisonContext CreateContext()
    {
      return new ComparisonContext();
    }

    private bool TryRegisterDifference(object source, object target, Difference difference)
    {
      if (Results.ContainsKey(target ?? source))
        return false;
      if (source!=null)
        Results.Add(source, difference);
      if (target!=null)
        Results.Add(target, difference);
      return true;
    }

    /// <summary>
    /// Gets the path of the target node.
    /// </summary>
    /// <param name="source">The source node.</param>
    /// <returns>The path of the target node.</returns>
    protected virtual string GetTargetPath(Node source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      if (source.Model==Source) {
        var renameHint = Hints.GetHint<RenameHint>(source);
        if (renameHint!=null)
          return renameHint.TargetPath;
        var parent = source.Parent;
        if (parent!=null) {
          string path = source.Path;
          string parentPath = parent.Path;
          string pathTail = path.Substring(parentPath.Length);
          return GetTargetPath(parent) + pathTail;
        }
      }
      return source.Path;
    }

    /// <summary>
    /// Gets the name of the target node.
    /// </summary>
    /// <param name="source">The source node.</param>
    /// <returns>The name of the target node.</returns>
    protected virtual string GetTargetName(Node source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      if (source.Model==Source) {
        var renameHint = Hints.GetHint<RenameHint>(source);
        if (renameHint!=null)
          return Target.Resolve(renameHint.TargetPath).Name;
      }
      return source.Name;
    }

    /// <summary>
    /// Gets the highest common base type.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object.</param>
    /// <returns>The highest common base type.</returns>
    protected Type GetCommonBase(object source, object target)
    {
      var sourceAncestors = GetAncestors(source==null ? typeof (object) : source.GetType());
      var targetAncestors = GetAncestors(target==null ? typeof (object) : target.GetType());
      var sourceType = sourceAncestors[sourceAncestors.Count - 1];
      var targetType = targetAncestors[targetAncestors.Count - 1];
      if (sourceType.IsAssignableFrom(targetType))
        return targetType;
      if (targetType.IsAssignableFrom(sourceType))
        return sourceType;
      var commonBase = typeof (object);
      for (int i = 0; i < Math.Min(sourceAncestors.Count, targetAncestors.Count); i++) {
        var ancestor = sourceAncestors[i];
        if (ancestor!=targetAncestors[i])
          break;
        commonBase = ancestor;
      }
      return commonBase;
    }

    private List<Type> GetAncestors(Type type)
    {
      var list = new List<Type>();
      while (type!=typeof(object)) {
        list.Insert(0, type);
        type = type.BaseType;
      }
      list.Insert(0, typeof(object));
      return list;
    }

    #endregion
  }
}