using System.Reflection;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Tracking.Tests;

public static class TestHelper
{
  private static readonly Type TrackingItemType = typeof (ITrackingItem).Assembly.GetType("Xtensive.Orm.Tracking.TrackingItem");
  private static readonly MethodInfo MergeWithMethod = TrackingItemType.GetMethod("MergeWith");

  public static void Merge(ITrackingItem target, ITrackingItem source) =>
    _ = MergeWithMethod.Invoke(target, new object[] {source});

  public static ITrackingItem CreateTrackingItem(Key key, TrackingItemState state)
  {
    var tuple = Tuple.Create(typeof (string));
    var diff = new DifferentialTuple(tuple);
    return (ITrackingItem) Activator.CreateInstance(TrackingItemType, key, state, diff);
  }
}
