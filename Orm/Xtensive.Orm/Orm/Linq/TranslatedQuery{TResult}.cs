// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.27

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Linq
{
  /// <summary>
  /// LINQ query translation result.
  /// </summary>
  /// <typeparam name="TResult">The type of the result.</typeparam>
  [Serializable]
  internal class TranslatedQuery<TResult> : TranslatedQuery
  {
    /// <summary>
    /// Materializer.
    /// </summary>
    public readonly Func<IEnumerable<Tuple>, Session, Dictionary<Parameter<Tuple>, Tuple>, ParameterContext, TResult> Materializer;

    /// <summary>
    /// Gets the tuple parameter bindings.
    /// </summary>
    public Dictionary<Parameter<Tuple>, Tuple> TupleParameterBindings { get; private set; }

    /// <summary>
    /// Gets the tuple parameters.
    /// </summary>
    public List<Parameter<Tuple>> TupleParameters { get; private set; }

    /// <inheritdoc/>
    public override sealed Delegate UntypedMaterializer
    {
      get { return Materializer; }
    }

    /// <summary>
    /// Executes the query in specified parameter context.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="parameterContext">The parameter context.</param>
    /// <returns>Query execution result.</returns>
    public TResult Execute(Session session, ParameterContext parameterContext)
    {
      return Materializer.Invoke(DataSource.GetRecordSet(session), session, TupleParameterBindings, parameterContext);
    }

#if NET45

    /// <summary>
    /// Executes the query in specified parameter context asynchronously.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="parameterContext">The parameter context.</param>
    /// <returns>Query execution result.</returns>
    public async Task<TResult> ExecuteAsync(Session session, ParameterContext parameterContext)
    {
      var recordSet = await DataSource.GetRecordSetAsync(session);
      return Materializer.Invoke(recordSet, session, TupleParameterBindings, parameterContext);
    }

#endif

    // Constructors

    /// <summary>
    ///	Initializes a new instance of this class.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <param name="materializer">The materializer.</param>
    public TranslatedQuery(ExecutableProvider dataSource, Func<IEnumerable<Tuple>, Session, Dictionary<Parameter<Tuple>, Tuple>, ParameterContext, TResult> materializer)
      : this(dataSource, materializer, new Dictionary<Parameter<Tuple>, Tuple>(), EnumerableUtils<Parameter<Tuple>>.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <param name="materializer">The materializer.</param>
    /// <param name="tupleParameterBindings">The tuple parameter bindings.</param>
    /// <param name="tupleParameters">The tuple parameters.</param>
   public TranslatedQuery(ExecutableProvider dataSource, Func<IEnumerable<Tuple>, Session, Dictionary<Parameter<Tuple>, Tuple>, ParameterContext, TResult> materializer, Dictionary<Parameter<Tuple>, Tuple> tupleParameterBindings, IEnumerable<Parameter<Tuple>> tupleParameters)
      : base(dataSource)
    {
      Materializer = materializer;
      TupleParameterBindings = new Dictionary<Parameter<Tuple>, Tuple>(tupleParameterBindings);
      TupleParameters = tupleParameters.ToList();
    }
  }
}