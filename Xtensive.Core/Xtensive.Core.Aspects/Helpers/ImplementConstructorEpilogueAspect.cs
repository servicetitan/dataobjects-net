// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Nick Svetlov
// Created:    2008.05.29
// Copyright (C) 2007 Xtensive LLC.


using System;
using System.Reflection;
using PostSharp.Extensibility;
using PostSharp.Laos;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Core.Aspects.Helpers
{
  /// <summary>
  /// Implements epilogue call in constructor.
  /// </summary>
  [MulticastAttributeUsage(MulticastTargets.Constructor)]
  [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
  [Serializable]
  public sealed class ImplementConstructorEpilogueAspect : LaosMethodLevelAspect
  {
    /// <summary>
    /// Gets the type where epilogue method is declared.
    /// </summary>
    public Type HandlerType { get; private set; }

    /// <summary>
    /// Gets the name of the epilogue method to call.
    /// </summary>
    public string HandlerMethodName { get; private set; }

    /// <inheritdoc/>
    public override bool CompileTimeValidate(MethodBase method)
    {
      if (!AspectHelper.ValidateMemberType(this, SeverityType.Error,
        method, true, MemberTypes.Constructor))
        return false;
      if (!AspectHelper.ValidateMethodAttributes(this, SeverityType.Error,
        method, false, MethodAttributes.Static))
        return false;
      if (!AspectHelper.ValidateBaseType(this, SeverityType.Error,
        method.DeclaringType, true, HandlerType))
        return false;

      MethodInfo targetMethod; 
      
      if (!AspectHelper.ValidateMethod(this, SeverityType.Error,
        HandlerType, true, 
        BindingFlags.Instance |
        BindingFlags.DeclaredOnly |
        BindingFlags.Public | 
        BindingFlags.NonPublic | 
        BindingFlags.ExactBinding,
        typeof(void), HandlerMethodName, new [] { typeof (Type)}, out targetMethod))
        return false;
      if (!AspectHelper.ValidateMethodAttributes(this, SeverityType.Error,
        targetMethod, false, MethodAttributes.Private))
        return false;
      if (!AspectHelper.ValidateMethodAttributes(this, SeverityType.Error,
        targetMethod, false, MethodAttributes.Virtual))
        return false;

      return true;
    }

    /// <inheritdoc/>
    public override PostSharpRequirements GetPostSharpRequirements()
    {
      PostSharpRequirements requirements = base.GetPostSharpRequirements();
      AspectHelper.AddStandardRequirements(requirements);
      return requirements;
    }

    /// <summary>
    /// Applies this aspect to the specified <paramref name="ctor"/>.
    /// </summary>
    /// <param name="ctor">The constructor to apply the aspect to.</param>
    /// <param name="handlerType"><see cref="HandlerType"/> property value.</param>
    /// <param name="handlerMethodName"><see cref="HandlerMethodName"/> property value.</param>
    /// <returns>If it was the first application with the specified set of arguments, the newly created aspect;
    /// otherwise, <see langword="null" />.</returns>
    public static ImplementConstructorEpilogueAspect ApplyOnce(ConstructorInfo ctor, Type handlerType, string handlerMethodName)
    {
      ArgumentValidator.EnsureArgumentNotNull(ctor, "ctor");
      ArgumentValidator.EnsureArgumentNotNull(handlerType, "handlerType");
      ArgumentValidator.EnsureArgumentNotNull(handlerMethodName, "handlerMethodName");

      return AppliedAspectSet.Add(new Triplet<ConstructorInfo, Type, string>(ctor, handlerType, handlerMethodName), 
        () => new ImplementConstructorEpilogueAspect(handlerType, handlerMethodName));
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="handlerType"><see cref="HandlerType"/> property value.</param>
    /// <param name="handlerMethodName"><see cref="HandlerMethodName"/> property value.</param>
    public ImplementConstructorEpilogueAspect(Type handlerType, string handlerMethodName)
    {
      HandlerType = handlerType;
      HandlerMethodName = handlerMethodName;
    }
  }
}