// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.12.11

using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Reflection;

namespace Xtensive.Orm.Linq.Model
{
  internal static class QueryParser
  {
    public static GroupByQuery ParseGroupBy(MethodCallExpression mc)
    {
      var method = mc.Method;
      var mcArguments = mc.Arguments;
      GenericMethodHandle methodInfoHandle = new(method);

      if (methodInfoHandle.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByHandle))
        return new GroupByQuery {
          Source = mcArguments[0],
          KeySelector = mcArguments[1].StripQuotes(),
        };

      if (methodInfoHandle.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByWithElementSelectorHandle))
        return new GroupByQuery {
          Source = mcArguments[0],
          KeySelector = mcArguments[1].StripQuotes(),
          ElementSelector = mcArguments[2].StripQuotes(),
        };

      if (methodInfoHandle.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByWithResultSelectorHandle))
        return new GroupByQuery {
            Source = mcArguments[0],
            KeySelector = mcArguments[1].StripQuotes(),
            ResultSelector = mcArguments[2].StripQuotes(),
          };

      if (methodInfoHandle.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByWithElementAndResultSelectorsHandle))
        return new GroupByQuery {
          Source = mcArguments[0],
          KeySelector = mcArguments[1].StripQuotes(),
          ElementSelector = mcArguments[2].StripQuotes(),
          ResultSelector = mcArguments[3].StripQuotes()
        };

      throw new NotSupportedException(string.Format(
        Strings.ExGroupByOverloadXIsNotSupported,
        mc.ToString(true)));
    }
  }
}
