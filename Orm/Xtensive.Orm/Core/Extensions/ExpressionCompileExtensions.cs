// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.05.07

using System.Linq.Expressions;
using Xtensive.Linq;

namespace Xtensive.Core;

/// <summary>
/// Extension methods for compiling strictly typed lambda expressions.
/// </summary>
public static class ExpressionCompileExtensions
{
  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<TResult> CachingCompile<TResult>(this Expression<Func<TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    var d = (Func<object[], TResult>) compiled;
    return () => d(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, TResult> CachingCompile<T1, TResult>(this Expression<Func<T1, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    var d = (Func<object[], T1, TResult>) compiled;
    return (arg2) => d(constants, arg2);
  }
}
