// Copyright (C) 2011 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2011.10.13

namespace Xtensive.Orm.Upgrade.Model;

[Serializable]
public readonly struct PartialIndexFilterInfo : IEquatable<PartialIndexFilterInfo>
{
  public string Expression { get; }

  public bool Equals(PartialIndexFilterInfo other) => Expression == other.Expression;
  public override bool Equals(object obj) => obj is PartialIndexFilterInfo other && Equals(other);
  public override int GetHashCode() => Expression.GetHashCode();

  public override string ToString() => Expression;


  // Constructors

  public PartialIndexFilterInfo(string expression)
  {
    ArgumentException.ThrowIfNullOrEmpty(expression);
    Expression = expression;
  }
}
