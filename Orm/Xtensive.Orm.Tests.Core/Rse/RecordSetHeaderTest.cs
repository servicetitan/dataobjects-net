// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xtensive.Orm.Rse;
using Xtensive.Tuples;

namespace Xtensive.Orm.Tests.Core.Rse
{
  /// <summary>
  /// Regression tests for <see cref="RecordSetHeader"/> ensuring that intermediate
  /// header construction during query translation does not eagerly trip the
  /// <c>DO_MAX_1000_COLUMNS</c> guard in <see cref="TupleDescriptor.LazyData"/>.
  /// The guard must still fire if/when the resulting descriptor's lazy data
  /// is actually accessed (e.g. at materialization).
  /// </summary>
  [TestFixture]
  public class RecordSetHeaderTest
  {
    private static RecordSetHeader CreateHeader(int columnCount, string prefix = "c")
    {
      var fieldTypes = new Type[columnCount];
      var columns = new Column[columnCount];
      for (var i = 0; i < columnCount; i++) {
        fieldTypes[i] = typeof(int);
        columns[i] = new SystemColumn(prefix + i, (ColNum) i, typeof(int));
      }
      return new RecordSetHeader(TupleDescriptor.CreateFromNormalized(fieldTypes), columns);
    }

    [Test]
    public void Add_DoesNotEagerlyValidateColumnCount()
    {
      var header = CreateHeader(1000);

      // Adding a single column would push the resulting descriptor over the
      // 1000-column threshold. Header construction must succeed lazily.
      var extended = header.Add(new SystemColumn("extra", 1000, typeof(int)));

      Assert.That(extended.Columns.Count, Is.EqualTo(1001));
      Assert.That(extended.TupleDescriptor.Count, Is.EqualTo(1001));
    }

    [Test]
    public void AddRange_DoesNotEagerlyValidateColumnCount()
    {
      var header = CreateHeader(800);
      var extra = new List<Column>(300);
      for (var i = 0; i < 300; i++) {
        extra.Add(new SystemColumn("extra" + i, (ColNum) (800 + i), typeof(int)));
      }

      var extended = header.Add(extra);

      Assert.That(extended.Columns.Count, Is.EqualTo(1100));
      Assert.That(extended.TupleDescriptor.Count, Is.EqualTo(1100));
    }

    [Test]
    public void Join_DoesNotEagerlyValidateColumnCount()
    {
      var left = CreateHeader(700, "l");
      var right = CreateHeader(700, "r");

      var joined = left.Join(right);

      Assert.That(joined.Columns.Count, Is.EqualTo(1400));
      Assert.That(joined.TupleDescriptor.Count, Is.EqualTo(1400));
    }

    [Test]
    public void OversizedDescriptor_StillThrowsOnLazyDataAccess()
    {
      // Building a descriptor with > 1000 fields via the lazy path must succeed,
      // but accessing the lazy data (e.g. ValuesLength) must surface the guard.
      var fieldTypes = new Type[1500];
      for (var i = 0; i < fieldTypes.Length; i++) {
        fieldTypes[i] = typeof(int);
      }
      var descriptor = TupleDescriptor.CreateFromNormalized(fieldTypes);

      Assert.That(descriptor.Count, Is.EqualTo(1500));
      Assert.Throws<NotSupportedException>(() => _ = descriptor.ValuesLength);
    }
  }
}
