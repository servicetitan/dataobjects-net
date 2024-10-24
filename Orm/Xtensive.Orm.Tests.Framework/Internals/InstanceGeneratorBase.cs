// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2008.01.21

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Xtensive.Core;

namespace Xtensive.Orm.Tests
{
  /// <summary>
  /// Base class for any random generator.
  /// </summary>
  /// <typeparam name="T">Type of instances to generate.</typeparam>
  [Serializable]
  public abstract class InstanceGeneratorBase<T>: 
    IInstanceGenerator<T>
  {
    private IInstanceGeneratorProvider provider;

    /// <inheritdoc/>
    public IInstanceGeneratorProvider Provider
    {
      [DebuggerStepThrough]
      get { return provider; }
    }

    /// <inheritdoc/>
    public abstract T GetInstance(Random random);

    /// <inheritdoc/>
    public IEnumerable<T> GetInstances(Random random, int? count)
    {
      for (int i = 0; !count.HasValue || i<count.Value; i++)
        yield return GetInstance(random);
    }

    #region IInstanceGeneratorBase members

    /// <inheritdoc/>
    object IInstanceGeneratorBase.GetInstance(Random random)
    {
      return GetInstance(random);
    }

    /// <inheritdoc/>
    IEnumerable IInstanceGeneratorBase.GetInstances(Random random, int? count)
    {
      return GetInstances(random, count);
    }

    #endregion

    
    // Constructors

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    /// <param name="provider">Instance generator provider this generator is bound to.</param>
    public InstanceGeneratorBase(IInstanceGeneratorProvider provider)
    {
      ArgumentNullException.ThrowIfNull(provider);
      this.provider = provider;
    }

    public virtual void OnDeserialization(object sender)
    {
      if (provider==null || provider.GetType()==typeof (InstanceGeneratorProvider))
        provider = InstanceGeneratorProvider.Default;
    }
  }
}