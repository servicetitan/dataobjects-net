#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Modelling.Comparison;

namespace Xtensive.Modelling.Actions.Extensions
{
  public static class ActionSequenceExtensions
  {
    /// <summary>
    /// Checks if all actions within the <see cref="IActionSequence"/> exclusively involve modifications of the specified subject type.
    /// </summary>
    /// <typeparam name="TSubjectType">The subject type for which modifications are checked.</typeparam>
    /// <param name="actionSequence">The sequence of actions to check.</param>
    /// <returns>True if the sequence contains only actions related to the creation or removal of nodes of the specified target type; otherwise, false.</returns>
    public static bool ContainsOnlyModificationOf<TSubjectType>(this IActionSequence actionSequence) =>
      ContainsActionsOfType<TSubjectType>(actionSequence, new[] { typeof(CreateNodeAction), typeof(RemoveNodeAction) });
    
    /// <summary>
    /// Checks if all actions within the <see cref="IActionSequence"/> are allowed for differences of the specified type.
    /// </summary>
    /// <typeparam name="TSubjectType">The subject type to check for in the differences associated with the actions.</typeparam>
    /// <param name="actionSequence">The sequence of actions to check.</param>
    /// <param name="types">A collection of types to compare with the types of the associated differences.</param>
    /// <returns>True if all actions are allowed for differences of the specified type; otherwise, false.</returns>
    public static bool ContainsActionsOfType<TSubjectType>(this IActionSequence actionSequence, IEnumerable<Type> types) =>
      actionSequence.Flatten().All(action => action.IsActionAllowedForDifference<TSubjectType>(types));
    
    /// <summary>
    /// Determines whether the specified action is allowed for a difference of the specified type.
    /// </summary>
    /// <typeparam name="TSubjectType">The subject type to check for in the difference.</typeparam>
    /// <param name="nodeAction">The <see cref="INodeAction"/> instance associated with the action.</param>
    /// <param name="types">A collection of types to compare with the type of the node action.</param>
    /// <returns>True if the action is allowed for a difference of the specified type; otherwise, false.</returns>
    private static bool IsActionAllowedForDifference<TSubjectType>(this INodeAction nodeAction, IEnumerable<Type> types)
    {
      if (nodeAction.Difference is null) {
        return false;
      }

      var isNodeActionOfType = types.Any(type => nodeAction.GetType() == type);

      return nodeAction.Difference.IsAllowedForSubject<TSubjectType>(isNodeActionOfType,
               static difference => difference?.Target) ||
             nodeAction.Difference.IsAllowedForSubject<TSubjectType>(isNodeActionOfType,
               static difference => difference?.Source);
    }
    
    /// <summary>
    /// Determines whether the given <see cref="IDifference"/> is allowed for a subject of the specified type.
    /// </summary>
    /// <typeparam name="TSubjectType">The subject type to check for in the subject.</typeparam>
    /// <param name="difference">The current difference.</param>
    /// <param name="isNodeActionOfType">A flag indicating if the action type of the node matches the specified type.</param>
    /// <param name="subject">A function to extract the subject of type <see cref="object"/> from the difference.</param>
    /// <returns>True if the difference is allowed for the specified subject type; otherwise, checks the parent chain and returns true if the type is found, otherwise false.</returns>
    private static bool IsAllowedForSubject<TSubjectType>(this IDifference difference,
      bool isNodeActionOfType,
      Func<IDifference?, object?> subject) =>
      (isNodeActionOfType && subject(difference) is TSubjectType) || difference.IsParentTargetOfType<TSubjectType>(subject);

    /// <summary>
    /// Checks whether the parent chain of the given <see cref="IDifference"/> contains a target of the specified type.
    /// </summary>
    /// <typeparam name="TSubjectType">The subject type to check for in the parent chain.</typeparam>
    /// <param name="difference">The current difference.</param>
    /// <param name="diffSubject">A function to extract the subject of type <see cref="object"/> from the parent difference.</param>
    /// <returns>True if a target of the specified type is found in the parent chain; otherwise, false.</returns>
    private static bool IsParentTargetOfType<TSubjectType>(this IDifference difference, Func<IDifference?, object?> diffSubject)
    {
      var parentDifference = difference.Parent;
      
      var subject = diffSubject(parentDifference);
      
      while (subject is not null) {
        if (subject is TSubjectType) {
          return true;
        }
      
        parentDifference = parentDifference.Parent;
        subject = diffSubject(parentDifference);
      }

      return false;
    }
  }
}