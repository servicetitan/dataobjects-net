// Copyright (C) 2003-2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.12.17

using System;
using Xtensive.Core;
using Xtensive.Sql;

namespace Xtensive.Orm.Providers
{
  internal readonly struct QueryParameterIdentity : IEquatable<QueryParameterIdentity>
  {
    public TypeMapping Mapping { get; }

    public object ClosureObject { get; }

    public string FieldName { get; }

    public QueryParameterBindingType BindingType { get; }

    public bool Equals(QueryParameterIdentity other) =>
      string.Equals(FieldName, other.FieldName)
        && ClosureObject.Equals(other.ClosureObject)
        && BindingType == other.BindingType
        && Mapping.Equals(other.Mapping);

    public override int GetHashCode() => HashCode.Combine(FieldName, ClosureObject, BindingType, Mapping);

    public static bool operator ==(in QueryParameterIdentity left, in QueryParameterIdentity right) => left.Equals(right);
    public static bool operator !=(in QueryParameterIdentity left, in QueryParameterIdentity right) => !left.Equals(right);

    public override bool Equals(object obj) => obj is QueryParameterIdentity other && Equals(other);

    // Constructors

    public QueryParameterIdentity(TypeMapping mapping, object closureObject, string fieldName, QueryParameterBindingType bindingType)
    {
      ArgumentNullException.ThrowIfNull(mapping);
      ArgumentNullException.ThrowIfNull(closureObject);
      ArgumentException.ThrowIfNullOrEmpty(fieldName);

      Mapping = mapping;
      ClosureObject = closureObject;
      FieldName = fieldName;
      BindingType = bindingType;
    }
  }
}
