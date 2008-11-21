// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.06.04

using System.Collections.Generic;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Internals
{
  internal static class FieldInfoExtensions
  {
    public static IList<ColumnInfo> ExtractColumns(this IEnumerable<FieldInfo> fields)
    {
      IList<ColumnInfo> result = new List<ColumnInfo>();
      foreach (FieldInfo fieldInfo in fields)
        ExtractColumns(fieldInfo, result);
      return result;
    }

    public static IList<ColumnInfo> ExtractColumns(this FieldInfo field)
    {
      IList<ColumnInfo> result = new List<ColumnInfo>();
      ExtractColumns(field, result);
      return result;
    }

    public static FieldAccessorBase<T> GetAccessor<T>(this FieldInfo field)
    {
      if (field.IsEntity)
        return EntityFieldAccessor<T>.Instance;
      if (field.IsStructure)
        return StructureFieldAccessor<T>.Instance;
      if (field.IsEnum)
        return EnumFieldAccessor<T>.Instance;
      if (field.IsEntitySet)
        return EntitySetFieldAccessor<T>.Instance;
      if (field.ValueType==typeof(Key))
        return KeyFieldAccessor<T>.Instance;
      return DefaultFieldAccessor<T>.Instance;
    }

    private static void ExtractColumns(FieldInfo field, ICollection<ColumnInfo> columns)
    {
      if (field.Column==null) {
        if (field.IsEntity)
          foreach (FieldInfo childField in field.Fields)
            ExtractColumns(childField, columns);
      }
      else
        columns.Add(field.Column);
    }
  }
}