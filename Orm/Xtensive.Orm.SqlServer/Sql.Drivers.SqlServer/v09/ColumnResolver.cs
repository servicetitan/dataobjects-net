// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.08.12

using Xtensive.Sql.Model;

namespace Xtensive.Sql.Drivers.SqlServer.v09
{
  internal sealed class ColumnResolver(DataTable table)
  {
    private readonly record struct ColumnIndexMapping(int DbIndex, int ModelIndex, bool IsValid);

    public DataTable Table = table;
    private List<ColumnIndexMapping> columnMappings;

    public void RegisterColumnMapping(int dbIndex, int modelIndex) =>
      (columnMappings ??= new(1)).Add(new ColumnIndexMapping(dbIndex, modelIndex, true));

    public DataTableColumn GetColumn(int dbIndex)
    {
      int modelIndex = dbIndex-1;
      if (Table is View view)
        return view.ViewColumns[modelIndex];

      var table = (Table)Table;
      if (columnMappings == null)
        return table.TableColumns[modelIndex];

      var mapping = columnMappings.FirstOrDefault(item => item.DbIndex==dbIndex);
      if (mapping != default)
        return table.TableColumns[mapping.ModelIndex];

      throw new ArgumentOutOfRangeException("dbIndex");
    }
  }
}
