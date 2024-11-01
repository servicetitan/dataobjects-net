// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.05.07

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Reflection;

using Factory = System.Func<
    System.Linq.Expressions.Expression,
    System.Collections.Generic.IReadOnlyList<System.Linq.Expressions.ParameterExpression>,
    System.Linq.Expressions.LambdaExpression
  >;

using SlowFactory = System.Func<
    System.Linq.Expressions.Expression,
    System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression>,
    System.Linq.Expressions.LambdaExpression
  >;

using FastFactory = System.Func<
    System.Linq.Expressions.Expression,
    string,
    bool,
    System.Collections.Generic.IReadOnlyList<System.Linq.Expressions.ParameterExpression>,
    System.Linq.Expressions.LambdaExpression
  >;

namespace Xtensive.Linq
{
  internal sealed class LambdaExpressionFactory
  {
    private static readonly Type[] internalFactorySignature = new[] {
      WellKnownTypes.Expression, WellKnownTypes.String, WellKnownTypes.Bool, typeof(IReadOnlyList<ParameterExpression>)
    };

    private static readonly Type FastFactoryType = typeof(FastFactory);
    private static readonly Type SlowFactoryType = typeof(SlowFactory);

    private static readonly MethodInfo SlowFactoryMethod = WellKnownTypes.Expression.GetMethods().Single(m =>
      m.IsGenericMethod &&
      m.Name == "Lambda" &&
      m.GetParameters()[1].ParameterType == typeof(IEnumerable<ParameterExpression>));

    private static readonly Func<Type, Factory> CreateHandler = CanUseFastFactory() ? CreateFactoryFast : CreateFactorySlow;

    public static LambdaExpressionFactory Instance { get; } = new();

    private readonly ConcurrentDictionary<Type, Factory> cache = new();

    public LambdaExpression CreateLambda(Type delegateType, Expression body, IReadOnlyList<ParameterExpression> parameters) =>
      cache.GetOrAdd(delegateType, CreateHandler).Invoke(body, parameters);

    public LambdaExpression CreateLambda(Expression body, IReadOnlyList<ParameterExpression> parameters)
    {
      var delegateType = DelegateHelper.MakeDelegateType(body.Type, parameters.Select(p => p.Type), parameters.Count);
      return CreateLambda(delegateType, body, parameters);
    }

    #region Private / internal methods

    internal static Factory CreateFactorySlow(Type delegateType)
    {
      var factory = (SlowFactory) Delegate.CreateDelegate(
        SlowFactoryType, SlowFactoryMethod.CachedMakeGenericMethod(delegateType));

      return (body, parameters) => factory.Invoke(body, parameters);
    }

    internal static Factory CreateFactoryFast(Type delegateType)
    {
      var method = WellKnownTypes.ExpressionOfT.CachedMakeGenericType(delegateType).GetMethod(
        "Create", BindingFlags.Static | BindingFlags.NonPublic, null, internalFactorySignature, null);

      if (method == null) {
        return null;
      }

      var factory = (FastFactory) Delegate.CreateDelegate(FastFactoryType, null, method);
      return (body, parameters) => factory.Invoke(body, null, false, parameters);
    }

    internal static bool CanUseFastFactory()
    {
      try {
        return CreateFactoryFast(typeof(Func<int>)) != null;
      }
      catch {
        return false;
      }
    }

    #endregion
  }
}
