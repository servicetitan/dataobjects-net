#nullable enable
using System;
using System.Linq.Expressions;

namespace Xtensive.Orm.Tests.Upgrade.SyncIndexes
{

  namespace V1
  {
    [HierarchyRoot]
    public class MyEntity : Entity
    {
      [Key, Field]
      public long Id { get; set; }
      
      [Field]
      public string? Name { get; set; }
      
      [Field]
      public long Value { get; set; }
      
      [Field]
      public long Count { get; set; }
    }
  }
  
  namespace V2
  {
    [Index(nameof(Value), Filter = nameof(CountIs), Name = "IX_Value")]
    [Index(nameof(Count))]
    [HierarchyRoot]
    public class MyEntity : Entity
    {
      private static Expression<Func<MyEntity, bool>> CountIs() => e => e.Count == 1;

      [Key, Field]
      public long Id { get; set; }
      
      [Field]
      public string? Name { get; set; }
      
      [Field]
      public long Value { get; set; }
      
      [Field]
      public long Count { get; set; }
    }
  }
  
  namespace V3
  {
    [Index(nameof(Value), Filter = nameof(CountIs), Name = "IX_Value")]
    [HierarchyRoot]
    public class MyEntity : Entity
    {
      private static Expression<Func<MyEntity, bool>> CountIs() => e => e.Count1 == 1;

      [Key, Field] 
      public long Id { get; set; }
      
      [Field]
      public string? Name { get; set; }

      [Field] 
      public long Value { get; set; }

      [Field] 
      public long Count1 { get; set; }
    }
  }
}
