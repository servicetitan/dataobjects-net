// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.05.06

using System.Linq.Expressions;

namespace Xtensive.Linq;

internal readonly struct ParameterExpressionRegistry()
{
  private readonly List<ParameterExpression> indexes = [];

  public int GetIndex(ParameterExpression parameter)
  {
    int result = indexes.IndexOf(parameter);
    if (result >= 0)
      return result;
    indexes.Add(parameter);
    return indexes.Count - 1;
  }

  public void Add(ParameterExpression parameter) => indexes.Add(parameter);

  public void AddRange(IEnumerable<ParameterExpression> parameters) => indexes.AddRange(parameters);

  public void Reset() => indexes.Clear();
}
