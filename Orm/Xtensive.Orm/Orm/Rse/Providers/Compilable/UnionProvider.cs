// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2009.04.01

namespace Xtensive.Orm.Rse.Providers;

public sealed class UnionProvider(CompilableProvider left, CompilableProvider right)
  : ConcatUnionBaseProvider(ProviderType.Union, left, right)
{
  #region Header build

  /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
  protected override void EnsureOperationIsPossible()
  {
    if (!Left.Header.TupleDescriptor.Equals(Right.Header.TupleDescriptor))
      throw new InvalidOperationException(String.Format(Strings.ExXCantBeExecuted, "Union operation"));
  }

  private static void EnsureUnionIsPossible(RecordSetHeader leftHeader, RecordSetHeader rightHeader)
  {
    var left = leftHeader.TupleDescriptor;
    var right = rightHeader.TupleDescriptor;
    if (!left.Equals(right)) {
      throw new InvalidOperationException(string.Format(Strings.ExXCantBeExecuted, "Union operation"));
    }
  }
  #endregion

  internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitUnion(this);
}
