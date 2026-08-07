// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.05.06

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Xtensive.Linq;

internal static class CachingExpressionCompiler
{
  private static class Traits<TDelegate>
  {
    public static readonly ConcurrentDictionary<ExpressionTree, Delegate> Cache = new();
  }

  internal static (TCompiledDelegate Compiled, object[] Constants) Compile<TDelegate, TCompiledDelegate>(Expression<TDelegate> lambda) where TCompiledDelegate : Delegate
  {
    var constantExtractor = new ConstantExtractor(lambda);
    var expressionTree = constantExtractor.Process().ToExpressionTree();
    return (
      (TCompiledDelegate) Traits<TDelegate>.Cache.GetOrAdd(expressionTree,
        static tree => ((LambdaExpression) tree.ToExpression()).Compile()),
      constantExtractor.GetConstants()
    );
  }
}
