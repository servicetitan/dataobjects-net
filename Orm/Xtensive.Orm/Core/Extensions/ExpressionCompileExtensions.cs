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
    return ((Func<object[], TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, TResult> CachingCompile<T1, TResult>(this Expression<Func<T1, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, TResult> CachingCompile<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, TResult> CachingCompile<T1, T2, T3, TResult>(this Expression<Func<T1, T2, T3, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, TResult> CachingCompile<T1, T2, T3, T4, TResult>(this Expression<Func<T1, T2, T3, T4, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, TResult> CachingCompile<T1, T2, T3, T4, T5, TResult>(this Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Func<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action CachingCompile(this Expression<Action> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[]>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1> CachingCompile<T1>(this Expression<Action<T1>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2> CachingCompile<T1, T2>(this Expression<Action<T1, T2>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3> CachingCompile<T1, T2, T3>(this Expression<Action<T1, T2, T3>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4> CachingCompile<T1, T2, T3, T4>(this Expression<Action<T1, T2, T3, T4>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5> CachingCompile<T1, T2, T3, T4, T5>(this Expression<Action<T1, T2, T3, T4, T5>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6> CachingCompile<T1, T2, T3, T4, T5, T6>(this Expression<Action<T1, T2, T3, T4, T5, T6>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7> CachingCompile<T1, T2, T3, T4, T5, T6, T7>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>) compiled).Bind(constants);
  }

  /// <summary>Compiles the specified lambda and caches the result of compilation.</summary>
  /// <returns>Compiled lambda.</returns>
  public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> CachingCompile<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>> lambda)
  {
    var (compiled, constants) = CachingExpressionCompiler.Compile(lambda);
    return ((Action<object[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>) compiled).Bind(constants);
  }
}
