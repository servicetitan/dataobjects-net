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
    var (compiled, constants) = CachingExpressionCompiler.Compile<Func<TResult>, Func<object[], TResult>>(lambda);
    return () => compiled(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, TResult> CachingCompile<T1, TResult>(this Expression<Func<T1, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile<Func<T1, TResult>, Func<object[], T1, TResult>>(lambda);
    return (arg1) => compiled(constants, arg1);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, TResult> CachingCompile<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile<Func<T1, T2, TResult>, Func<object[], T1, T2, TResult>>(lambda);
    return (arg1, arg2) => compiled(constants, arg1, arg2);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, TResult> CachingCompile<T1, T2, T3, TResult>(this Expression<Func<T1, T2, T3, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile<Func<T1, T2, T3, TResult>, Func<object[], T1, T2, T3, TResult>>(lambda);
    return (arg1, arg2, arg3) => compiled(constants, arg1, arg2, arg3);
  }
}
