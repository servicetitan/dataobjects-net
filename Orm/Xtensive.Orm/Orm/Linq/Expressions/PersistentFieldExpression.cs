// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;

namespace Xtensive.Orm.Linq.Expressions
{
  internal abstract class PersistentFieldExpression : ParameterizedExpression
  {
    internal Segment<ColNum> Mapping;

    public string Name { get; private set; }
    public PropertyInfo UnderlyingProperty { get; private set; }
    public override PersistentFieldExpression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions) => throw new NotImplementedException();

    public override string ToString()
    {
      return $"{base.ToString()} {Name}, {Mapping}";
    }


    // Constructors

    protected PersistentFieldExpression(
      ExtendedExpressionType expressionType, 
      string name, 
      Type type, 
      in Segment<ColNum> segment,
      PropertyInfo underlyingProperty,
      ParameterExpression parameterExpression,
      bool defaultIfEmpty)
      : base(expressionType, type, parameterExpression, defaultIfEmpty)
    {
      Name = name;
      UnderlyingProperty = underlyingProperty;
      Mapping = segment;
    }
  }
}