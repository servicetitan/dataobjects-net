// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.07.03

using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Type, TypeMapping> mappings;
    public IReadOnlyDictionary<Type, TypeMapping> Mappings => mappings;

    public IReadOnlyDictionary<SqlType, Type> ReverseMappings { get; }
    public TypeMapper Mapper { get; }

    public TypeMapping this[Type type] => GetMapping(type);

    public TypeMapping TryGetMapping(Type type) =>
       mappings.GetOrAdd(type, static (t, d) => t.IsEnum ? d.TryGetMapping(Enum.GetUnderlyingType(t)) : null, this);

    public TypeMapping GetMapping(Type type) =>
      TryGetMapping(type) ?? throw new NotSupportedException(string.Format(Strings.ExTypeXIsNotSupported, type.GetFullName()));

    /// <summary>
    /// Converts the specified <see cref="SqlType"/> to corresponding .NET type.
    /// </summary>
    /// <param name="sqlType">The type to convert.</param>
    /// <returns>Converter type.</returns>
    public Type MapSqlType(SqlType sqlType) =>
      ReverseMappings.TryGetValue(sqlType, out var type)
        ? type
        : throw new NotSupportedException(string.Format(Strings.ExTypeXIsNotSupported, sqlType.Name));

    // Constructors

    public TypeMappingRegistry(IEnumerable<TypeMapping> mappings, IEnumerable<KeyValuePair<SqlType, Type>> reverseMappings, TypeMapper mapper)
    {
      this.mappings = new(mappings.Select(m => KeyValuePair.Create(m.Type, m)));
      ReverseMappings = reverseMappings.ToDictionary(r => r.Key, r => r.Value).AsSafeWrapper();
      Mapper = mapper;
    }
  }
}
