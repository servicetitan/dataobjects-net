// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.12.11

using System;
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

      if (method.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByParams))
        return new GroupByQuery {
          Source = mcArguments[0],
          KeySelector = mcArguments[1].StripQuotes(),
        };

      if (method.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByWithElementSelectorParams))
        return new GroupByQuery {
          Source = mcArguments[0],
          KeySelector = mcArguments[1].StripQuotes(),
          ElementSelector = mcArguments[2].StripQuotes(),
        };

      if (method.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByWithResultSelectorParams))
        return new GroupByQuery {
            Source = mcArguments[0],
            KeySelector = mcArguments[1].StripQuotes(),
            ResultSelector = mcArguments[2].StripQuotes(),
          };

      if (method.IsGenericMethodSpecificationOf(QueryableMethodInfo.GroupByWithElementAndResultSelectorsParams))
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
