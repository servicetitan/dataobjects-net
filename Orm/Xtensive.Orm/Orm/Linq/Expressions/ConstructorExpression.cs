// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2009.10.16

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Orm.Linq.Expressions;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq
{
  [Serializable]
  internal sealed class ConstructorExpression : ParameterizedExpression
  {
    public Dictionary<MemberInfo, Expression> Bindings { get; }

    public Dictionary<MemberInfo, Expression> NativeBindings { get; private set; }

    public ConstructorInfo Constructor { get; }

    public IReadOnlyList<Expression> ConstructorArguments { get; }

    public override ParameterizedExpression BindParameter(ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      GenericExpressionVisitor<IMappedExpression> genericVisitor = new(mapped => mapped.BindParameter(parameter, processedExpressions));
      var genericBinder = genericVisitor.Process;
      return new ConstructorExpression(
        Type,
        Bindings.ToDictionary(kvp => kvp.Key, kvp => genericBinder(kvp.Value), Bindings.Count),
        NativeBindings.ToDictionary(kvp=>kvp.Key, kvp => genericBinder(kvp.Value), NativeBindings.Count),
        Constructor,
        ConstructorArguments.Select(genericBinder).ToArray());
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      GenericExpressionVisitor<IMappedExpression> genericVisitor = new(mapped => mapped.RemoveOuterParameter(processedExpressions));
      var genericRemover = genericVisitor.Process;
      var result = new ConstructorExpression(
        Type,
        Bindings.ToDictionary(kvp => kvp.Key, kvp => genericRemover(kvp.Value)),
        NativeBindings = NativeBindings.ToDictionary(kvp => kvp.Key, kvp => genericRemover(kvp.Value)),
        Constructor,
        ConstructorArguments.Select(genericRemover).ToArray());
      return result;
    }

    public override Expression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      Func<IMappedExpression, Expression> remapper = delegate(IMappedExpression mapped) {
        var parametrizedExpression = mapped as ParameterizedExpression;
        if (parametrizedExpression!=null && (parametrizedExpression.OuterParameter==OuterParameter || OuterParameter==null))
          return mapped.Remap(offset, new Dictionary<Expression, Expression>());
        return (Expression) mapped;
      };
      GenericExpressionVisitor<IMappedExpression> genericVisitor = new(remapper);
      var newBindings = Bindings.ToDictionary(kvp => kvp.Key, kvp => genericVisitor.Process(kvp.Value));
      var newConstructorArguments = ConstructorArguments.Select(genericVisitor.Process).ToArray();
      var newNativeBindings = NativeBindings.ToDictionary(kvp => kvp.Key, kvp => genericVisitor.Process(kvp.Value));
      var result = new ConstructorExpression(
        Type,
        newBindings,
        newNativeBindings,
        Constructor,
        newConstructorArguments);
      return result;
    }

    public override Expression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      Func<IMappedExpression, Expression> remapper = delegate(IMappedExpression mapped) {
        var parametrizedExpression = mapped as ParameterizedExpression;
        if (parametrizedExpression!=null && (parametrizedExpression.OuterParameter==OuterParameter || OuterParameter==null))
          return mapped.Remap(map, new Dictionary<Expression, Expression>());
        return (Expression) mapped;
      };
      GenericExpressionVisitor<IMappedExpression> genericVisitor = new(remapper);
      var newBindings = Bindings.ToDictionary(kvp => kvp.Key, kvp => genericVisitor.Process(kvp.Value));
      var newConstructorArguments = ConstructorArguments.Select(genericVisitor.Process).ToArray();
      var newNativeBindings = NativeBindings.ToDictionary(kvp => kvp.Key, kvp => genericVisitor.Process(kvp.Value));
      return new ConstructorExpression(Type, newBindings, newNativeBindings, Constructor, newConstructorArguments);
    }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitConstructorExpression(this);

    public ConstructorExpression(Type type, Dictionary<MemberInfo, Expression> bindings, Dictionary<MemberInfo, Expression> nativeBindings, ConstructorInfo constructor, IReadOnlyList<Expression> constructorArguments)
      : base(ExtendedExpressionType.Constructor, type, null, false)
    {
      Bindings = bindings ?? new Dictionary<MemberInfo, Expression>();
      NativeBindings = nativeBindings;
      ConstructorArguments = constructorArguments ?? Array.Empty<Expression>();
      Constructor = constructor;
    }
  }
}
