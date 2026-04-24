// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using Xtensive.Orm;

namespace Xtensive.Orm.Tests.Linq.Optimization.Model
{
  /// <summary>
  /// A small self-contained fixture re-used by every translator-optimization test.
  /// The model intentionally exposes both required and nullable navigation
  /// properties so tests can exercise INNER/LEFT JOIN paths.
  /// </summary>
  [HierarchyRoot]
  public class Customer : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field(Length = 128)]
    public string Name { get; set; }

    [Field]
    public bool IsActive { get; set; }
  }

  [HierarchyRoot]
  public class Workflow : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field(Length = 64)]
    public string Name { get; set; }
  }

  /// <summary>
  /// Non-intersecting facet interfaces intentionally split <see cref="Order"/>'s
  /// shape so that generic query-building extensions can constrain on a single
  /// aspect (publishability / code-ownership / activation) without exposing
  /// the full entity surface. Their member sets do not overlap — a predicate
  /// typed against one cannot blindly see members declared on another.
  /// </summary>
  public interface IHasCode
  {
    string Code { get; }
  }

  public interface IHasPublishDate
  {
    DateTime? PublishedOn { get; }
  }

  public interface IHasActivation
  {
    bool IsActive { get; }
  }

  [HierarchyRoot]
  public class Order : Entity, IHasCode, IHasPublishDate, IHasActivation
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field(Length = 64)]
    public string Code { get; set; }

    [Field]
    public bool IsActive { get; set; }

    [Field]
    public DateTime? PublishedOn { get; set; }

    /// <summary>Nullable navigation — exercises LEFT JOIN.</summary>
    [Field]
    public Customer Customer { get; set; }

    /// <summary>Nullable navigation — exercises LEFT JOIN.</summary>
    [Field]
    public Workflow Workflow { get; set; }

    [Field]
    [Association(PairTo = nameof(OrderItem.Order))]
    public EntitySet<OrderItem> Items { get; private set; }
  }

  [HierarchyRoot]
  public class OrderItem : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public Order Order { get; set; }

    [Field(Length = 128)]
    public string Name { get; set; }

    [Field]
    public int Quantity { get; set; }

    [Field]
    public decimal Price { get; set; }
  }
}
