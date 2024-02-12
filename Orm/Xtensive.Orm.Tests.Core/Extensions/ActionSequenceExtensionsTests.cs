#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Modelling.Actions;
using Xtensive.Modelling.Actions.Extensions;
using Xtensive.Modelling.Comparison;
using Xtensive.Modelling.Comparison.Hints;
using Xtensive.Orm.Upgrade.Model;
using Xtensive.Sql;
using Comparer = Xtensive.Modelling.Comparison.Comparer;

namespace Xtensive.Extensions
{
  public class ActionSequenceExtensionsTests
  {
    [Test]
    [TestCaseSource(nameof(AddSecondaryIndexTestCases))]
    public void ContainsActionsOfType_WhenCreatedIndex_ReturnsExpectedResult(IEnumerable<Type> types, bool expectedResult)
    {
      // Arrange
      var originalStorage = CreateStorage();
      var originalTable = CreateTable(originalStorage);
      var originalColumn = AddColumn<string>(originalTable);
      
      var actionSequence = GetActionSequence(originalStorage, (storage, _) => {
        var table = storage.Tables[originalTable.Name];
        var column = table.Columns[originalColumn.Name];
        AddSecondaryIndex(column);
      });

      // Act
      var result =
        actionSequence.ContainsActionsOfType<SecondaryIndexInfo>(types);
      
      // Assert
      Assert.AreEqual(result, expectedResult);
    }
    
    [Test]
    [TestCaseSource(nameof(RemoveSecondaryIndexTestCases))]
    public void ContainsActionsOfType_WhenRemovedIndex_ReturnsExpectedResult(IEnumerable<Type> types, bool expectedResult)
    {
      // Arrange
      var originalStorage = CreateStorage();
      var originalTable = CreateTable(originalStorage);
      var originalColumn = AddColumn<string>(originalTable);
      var originalIndex = AddSecondaryIndex(originalColumn);
      
      var actionSequence = GetActionSequence(originalStorage, (storage, _) => {
        var table = storage.Tables[originalTable.Name];
        var index = table.SecondaryIndexes[originalIndex.Name];
        table.SecondaryIndexes.Remove(index);
      });

      // Act
      var result =
        actionSequence.ContainsActionsOfType<SecondaryIndexInfo>(types);
      
      // Assert
      Assert.AreEqual(result, expectedResult);
    }

    [Test]
    [TestCaseSource(nameof(AllActionsTestCaseData))]
    public void ContainsActionsOfType_WhenChangedOtherNodeThenExpected_ReturnsFalse(Type actionType)
    {
      // Arrange
      var originalStorage = CreateStorage();
      var originalTable = CreateTable(originalStorage);
      
      var actionSequence = GetActionSequence(originalStorage, (storage, _) => {
        var table = storage.Tables[originalTable.Name];
        AddColumn<long>(table);
      });

      // Act
      var result =
        actionSequence.ContainsActionsOfType<SecondaryIndexInfo>(new[] { actionType });
      
      // Assert
      Assert.IsFalse(result);
    }    
    
    [Test]
    [TestCaseSource(nameof(DisallowedStorageChangesTestCaseData))]
    public void ContainsActionsOfType_WhenDisallowedStorageChangesLogged_ReturnsFalse(Action<StorageModel> action)
    {
      // Arrange
      var originalStorage = CreateStorage();
      CreateTable(originalStorage);
      
      var actionSequence = GetActionSequence(originalStorage, (storage, _) => {
        action(storage);
      });

      // Act
      var result =
        actionSequence.ContainsActionsOfType<SecondaryIndexInfo>(new[] {
          typeof(CreateNodeAction), typeof(RemoveNodeAction)
        });
      
      // Assert
      Assert.IsFalse(result);
    }
    
    [Test]
    [TestCaseSource(nameof(DisallowedTableChangesTestCaseData))]
    public void ContainsActionsOfType_WhenDisallowedTableActionsLogged_ReturnsFalse(Action<TableInfo> action)
    {
      // Arrange
      var originalStorage = CreateStorage();
      var originalTable = CreateTable(originalStorage);
      AddColumn<long>(originalTable);
      AddColumn<string>(originalTable);
      
      var actionSequence = GetActionSequence(originalStorage, (storage, _) => {
        var table = storage.Tables[originalTable.Name];
        action(table);
      });

      // Act
      var result =
        actionSequence.ContainsActionsOfType<SecondaryIndexInfo>(new[] {
          typeof(CreateNodeAction), typeof(RemoveNodeAction)
        });
      
      // Assert
      Assert.IsFalse(result);
    }

    private static StorageModel CreateStorage() => new("storage");

    private static TableInfo CreateTable(StorageModel storageInfo)
    {
      var table = new TableInfo(storageInfo, Guid.NewGuid().ToString());
      var id = new StorageColumnInfo(table, "Id", new StorageTypeInfo(typeof (int), new SqlValueType(SqlType.Int32)));
      var pk = new PrimaryIndexInfo(table, "PK_A");
      _ = new KeyColumnRef(pk, id);
      pk.PopulateValueColumns();

      return table;
    }

    private static StorageColumnInfo AddColumn<TType>(TableInfo table, string? name = null)
    {
      var column = new StorageColumnInfo(table, name ?? Guid.NewGuid().ToString(),
        new StorageTypeInfo(typeof(TType), new SqlValueType(typeof(TType).Name), false));

      var pk = table.PrimaryIndex;
      pk.ValueColumns.Clear();
      pk.PopulateValueColumns();

      return column;
    }

    private static SecondaryIndexInfo AddSecondaryIndex(params StorageColumnInfo[] columns)
    {
      var table = columns[0].Parent;
      var index = new SecondaryIndexInfo(table, $"index{Guid.NewGuid()}");

      foreach (var column in columns) {
        _ = new KeyColumnRef(index, column);
      }
      index.PopulatePrimaryKeyColumns();

      return index;
    }
    
    private static ActionSequence GetActionSequence(StorageModel origin, Action<StorageModel, HintSet> mutator)
    {
      var clonedStorage = Clone(origin);
      var hints = new HintSet(origin, clonedStorage);
      mutator.Invoke(clonedStorage, hints);
      origin.Validate();
      clonedStorage.Validate();

      var comparer = new Comparer();
      var diff = comparer.Compare(origin, clonedStorage, hints);
      return new ActionSequence() {
        new Upgrader().GetUpgradeSequence(diff, hints, comparer)
      };
    }

    private static StorageModel Clone(StorageModel storage) => (StorageModel) storage.Clone(null, storage.Name);

    private static readonly IEnumerable<Type> AllNodeActionsBesidesGroup = Assembly.GetAssembly(typeof(NodeAction))!
      .DefinedTypes
      .Where(type => type.IsSubclassOf(typeof(NodeAction))).Except(new[] { typeof(GroupingNodeAction) });

    private static readonly IEnumerable<Type> AllActionBesidesCreateAndRemove =
      AllNodeActionsBesidesGroup.Except(new[] { typeof(CreateNodeAction), typeof(RemoveNodeAction) });
    
    public static IEnumerable AddSecondaryIndexTestCases
    {
      get {
        yield return new TestCaseData(new object?[] { new [] { typeof(CreateNodeAction) }, true });
        yield return new TestCaseData(new object?[] { new [] { typeof(RemoveNodeAction) }, false });
        yield return new TestCaseData(new object?[] { new [] { typeof(CreateNodeAction), typeof(RemoveNodeAction) }, true });
        yield return new TestCaseData(new object?[] { AllActionBesidesCreateAndRemove, false });
      }
    }
    
    public static IEnumerable RemoveSecondaryIndexTestCases
    {
      get {
        yield return new TestCaseData(new object?[] { new [] { typeof(CreateNodeAction) }, false });
        yield return new TestCaseData(new object?[] { new [] { typeof(RemoveNodeAction) }, true });
        yield return new TestCaseData(new object?[] { new [] { typeof(CreateNodeAction), typeof(RemoveNodeAction) }, true });
        yield return new TestCaseData(new object?[] { AllActionBesidesCreateAndRemove, false });
      }
    }
    
    
    public static IEnumerable AllActionsTestCaseData => AllNodeActionsBesidesGroup.Select(actionType => new TestCaseData(actionType));

    public static IEnumerable DisallowedStorageChangesTestCaseData
    {
      get {
        yield return new TestCaseData(new object?[] { new Action<StorageModel>(storageInfo => CreateTable(storageInfo)) });
        yield return new TestCaseData(new object?[] { new Action<StorageModel>(storageInfo => storageInfo.Tables.First().Remove()) });
      }
    }
    
    public static IEnumerable DisallowedTableChangesTestCaseData
    {
      get {
        yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => table.PrimaryIndex.Remove()) });
        // TODO: possibly not supported
        // yield return new TestCaseData(new object?[] {
        //   new Action<TableInfo>(table => {
        //     table.PrimaryIndex.KeyColumns.First().Remove();
        //     new KeyColumnRef(table.PrimaryIndex, table.Columns.Last());
        //     table.PrimaryIndex.ValueColumns.Clear();
        //     table.PrimaryIndex.PopulateValueColumns();
        //   })
        // });
        yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => table.PrimaryIndex.Name = $"OtherName{Guid.NewGuid() }") });
        yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => AddColumn<string>(table)) });
        yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => {
          table.Columns.Last().Remove();
          table.PrimaryIndex.ValueColumns.Clear();
          table.PrimaryIndex.PopulateValueColumns();
        }) });
        yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => table.Columns.Last().Name = $"OtherName{Guid.NewGuid()}") });
        // TODO: DefaultValue is marked as IgnoreInComparison
        //yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => table.Columns.Last(c => c.Type.Type == typeof(string)).DefaultValue = $"") });
        yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => table.Columns.Last().Type = new StorageTypeInfo(typeof(byte), new SqlValueType(SqlType.Int8))) });
        // TODO: seems like index change not handled in upgrade
        // yield return new TestCaseData(new object?[] { new Action<TableInfo>(table => {
        //   table.Columns.Last().Index -= 1;
        //   table.PrimaryIndex.ValueColumns.Clear();
        //   table.PrimaryIndex.PopulateValueColumns();
        // }) });
      }
    }
  }
}