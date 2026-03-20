// Copyright (C) 2011 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2011.10.27

using System.Linq.Expressions;
using System.Reflection;

namespace Xtensive.Orm.Configuration
{
  /// <summary>
  /// Registration entry for LINQ extension.
  /// </summary>
  public readonly struct LinqExtensionRegistration
  {
    /// <summary>
    /// Gets member this extension is intended for.
    /// </summary>
    public MemberInfo Member { get; }

    /// <summary>
    /// Gets substitution that is performed when LINQ translator encouters <see cref="Member"/> access.
    /// </summary>
    public LambdaExpression Substitution { get; }

    /// <summary>
    /// Gets action that is performed when LINQ translator encouters <see cref="Member"/> access.
    /// </summary>
    public Func<MemberInfo, Expression, Expression[], Expression> Compiler { get; }


    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="member">Value for <see cref="Member"/>.</param>
    /// <param name="substitution">Value for <see cref="Substitution"/>.</param>
    public LinqExtensionRegistration(MemberInfo member, LambdaExpression substitution)
    {
      ArgumentNullException.ThrowIfNull(member);
      ArgumentNullException.ThrowIfNull(substitution);

      Member = member;
      Substitution = substitution;
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="member">Value for <see cref="Member"/>.</param>
    /// <param name="compiler">Value for <see cref="Compiler"/>.</param>
    public LinqExtensionRegistration(MemberInfo member, Func<MemberInfo, Expression, Expression[], Expression> compiler)
    {
      ArgumentNullException.ThrowIfNull(member);
      ArgumentNullException.ThrowIfNull(compiler);

      Member = member;
      Compiler = compiler;
    }
  }
}
