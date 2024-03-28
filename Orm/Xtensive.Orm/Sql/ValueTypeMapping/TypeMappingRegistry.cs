// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.07.03

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Reflection;

namespace Xtensive.Sql
{
  /// <summary>
  /// A collection of <see cref="TypeMapping"/> objects.
  /// </summary>
  public readonly struct TypeMappingRegistry
  {
    public IReadOnlyDictionary<Type, TypeMapping> Mappings { get; }
    public IReadOnlyDictionary<SqlType, Type> ReverseMappings { get; }
    public TypeMapper Mapper { get; }

    public TypeMapping this[Type type] { get { return GetMapping(type); } }
    
    public TypeMapping TryGetMapping(Type type) =>
      Mappings.GetValueOrDefault(type.IsEnum ? Enum.GetUnderlyingType(type) : type);

    public TypeMapping GetMapping(Type type)
    {
      var result = TryGetMapping(type);
      if (result==null)
        throw new NotSupportedException(string.Format(
          Strings.ExTypeXIsNotSupported, type.GetFullName()));
      return result;
    }

    /// <summary>
    /// Converts the specified <see cref="SqlType"/> to corresponding .NET type.
    /// </summary>
    /// <param name="sqlType">The type to convert.</param>
    /// <returns>Converter type.</returns>
    public Type MapSqlType(SqlType sqlType)
    {
      Type type;
      if (!ReverseMappings.TryGetValue(sqlType, out type))
        throw new NotSupportedException(string.Format(
          Strings.ExTypeXIsNotSupported, sqlType.Name));
      return type;
    }

    // Constructors

    public TypeMappingRegistry(IEnumerable<TypeMapping> mappings, IEnumerable<KeyValuePair<SqlType, Type>> reverseMappings, TypeMapper mapper)
    {
      Mappings = mappings.ToDictionary(m => m.Type).AsSafeWrapper();
      ReverseMappings = reverseMappings.ToDictionary(r => r.Key, r => r.Value).AsSafeWrapper();
      Mapper = mapper;
    }
  }
}