// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2009.04.01

using Xtensive.Collections;

namespace Xtensive.Orm.Rse.Providers;

/// <summary>
/// Produces union between <see cref="BinaryProvider.Left"/> and 
/// <see cref="BinaryProvider.Right"/> sources.
/// </summary>
[Serializable]
public sealed class UnionProvider(CompilableProvider left, CompilableProvider right)
  : ConcatUnionBaseProvider(ProviderType.Union, left, right)
{
  /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
  protected override void EnsureOperationIsPossible()
  {
    if (!Left.Header.TupleDescriptor.Equals(Right.Header.TupleDescriptor))
      throw new InvalidOperationException(String.Format(Strings.ExXCantBeExecuted, "Union operation"));
  }

  internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitUnion(this);
}
