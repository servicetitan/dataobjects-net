// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.04.17

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Rse.Transformation
{
  /// <summary>
  /// An expression visitor specialized for finding tuple access expressions.
  /// </summary>
  public class TupleAccessGatherer : ExpressionVisitor
  {
    private ParameterExpression tupleParameter;
    protected readonly Action<ApplyParameter, ColNum> registerOuterColumn;
    protected List<ColNum> mappings;

    /// <inheritdoc/>
    protected override Expression VisitUnknown(Expression e)
    {
      return e;
    }

    /// <inheritdoc/>
    protected override Expression VisitMethodCall(MethodCallExpression mc)
    {
      if (mc.IsTupleAccess(tupleParameter)) {
        var columnIndex = mc.GetTupleAccessArgument();
        var outerParameter = mc.GetApplyParameter();
        if (outerParameter!=null)
          registerOuterColumn(outerParameter, columnIndex);
        else
          mappings.Add(columnIndex);
        return mc;
      }
      return base.VisitMethodCall(mc);
    }

    /// <summary>
    /// Gathers used columns from specified <see cref="Expression"/>.
    /// </summary>
    /// <param name="expression">The predicate.</param>
    /// <returns>List containing all used columns (order and uniqueness are not guaranteed).</returns>
    public virtual IReadOnlyList<ColNum> Gather(Expression expression)
    {
      mappings = new List<ColNum>();
      Visit(expression);
      var result = mappings;
      mappings = null;
      return result;
    }

    /// <summary>
    /// Gathers used columns from specified <see cref="Expression"/>.
    /// </summary>
    /// <param name="expression">The predicate.</param>
    /// <param name="parameter">The tuple parameter to be considered.</param>
    /// <returns>List containing all used columns (order and uniqueness are not guaranteed).</returns>
    public IReadOnlyList<ColNum> Gather(Expression expression, ParameterExpression parameter)
    {
      ArgumentNullException.ThrowIfNull(expression);
      ArgumentNullException.ThrowIfNull(parameter);
      tupleParameter = parameter;
      var result = Gather(expression);
      tupleParameter = null;
      return result;
    }

    private static void DefaultRegisterOuterColumn(ApplyParameter parameter, ColNum columnIndex)
    {
    }

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    public TupleAccessGatherer()
      : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="registerOuterColumn">A <see langword="delegate"/> invoked on each outer column usage.</param>
    public TupleAccessGatherer(Action<ApplyParameter, ColNum> registerOuterColumn)
    {
      this.registerOuterColumn = registerOuterColumn ?? DefaultRegisterOuterColumn;
    }
  }
}