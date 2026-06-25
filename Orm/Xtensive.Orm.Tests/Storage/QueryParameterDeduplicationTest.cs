// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Tests.Storage.ClosureParametersCachingTestModel;

namespace Xtensive.Orm.Tests.Storage
{
  [TestFixture]
  public class QueryParameterDeduplicationTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.RegisterCaching(typeof(Invoice).Assembly, typeof(Invoice).Namespace);
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    protected override void PopulateData()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();
      _ = new PaymentSplit {
        Active = true,
        Payment = new Payment { Active = true },
        Invoice = new Invoice(),
        Order = 5,
        Amount = 10m
      };
      tx.Complete();
    }

    [Test]
    public void ClosureParameterReferencedMultipleTimesInQueryIsBoundOnce()
    {
      // `threshold` is referenced in two separate Where clauses, each compiled by its own ExpressionProcessor.
      // Without cross-processor dedup the same int value would appear as two DB parameters.
      var threshold = 1;
      AssertNoDuplicateParameterValues(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Order > threshold)
          .Where(split => split.Order >= threshold)
          .ToList();
      }, parameter => parameter.Value is int intValue && intValue == threshold, minSqlReferences: 2);
    }

    [Test]
    public void TypeIdReferencedInBothUnionBranchesIsBoundOnce()
    {
      // Each branch filters PaymentSplit independently; TypeId should still be a single binding.
      AssertNoDuplicateParameterValues(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Order > 10)
          .Union(session.Query.All<PaymentSplit>().Where(split => split.Order < 0))
          .ToList();
      }, parameter => parameter.Value is int intValue && intValue == GetPaymentSplitTypeId(), minSqlReferences: 2);
    }

    [Test]
    public void SmallRowFilterArrayReferencedInFilterAndProjectionIsBoundOncePerValue()
    {
      // `orders` is used in Where and again in Select; each distinct order value must appear once.
      var orders = new[] { 5, 7, 9 };
      AssertNoDuplicateParameterValues(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => orders.Contains(split.Order))
          .Select(split => new {
            split.Id,
            InFilter = orders.Contains(split.Order),
            StillInFilter = orders.Contains(split.Order)
          })
          .ToList();
      }, parameter => parameter.Value is int intValue && orders.Contains(intValue), minSqlReferences: 2);
    }

    [Test]
    public void SmallRowFilterArrayReferencedInBothUnionBranchesIsBoundOncePerValue()
    {
      // The same `orders` closure is evaluated in both sides of Union.
      var orders = new[] { 5, 7 };
      AssertNoDuplicateParameterValues(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => orders.Contains(split.Order))
          .Union(session.Query.All<PaymentSplit>().Where(split => orders.Contains(split.Order)))
          .ToList();
      }, parameter => parameter.Value is int intValue && orders.Contains(intValue), minSqlReferences: 2);
    }

    [Test]
    public void LargeRowFilterCollectionReferencedMultipleTimesUsesDistinctParameterValues()
    {
      Require.AllFeaturesSupported(ProviderFeatures.TableValuedParameters);

      // One large Contains list used in two Where clauses. With TVP dedup a single @tvp parameter is
      // created and referenced twice in the SQL. Without dedup two separate TVP parameters appear,
      // each referenced once. ParameterValueComparer cannot compare DataTable/SqlDataRecord values
      // reliably, so we instead verify that some parameter is referenced more than once in the SQL text.
      var orders = Enumerable.Range(1, Domain.Configuration.MaxNumberOfConditions + 50).ToArray();
      ExecuteQuery(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => orders.Contains(split.Order))
          .Where(split => orders.Contains(split.Order))
          .ToList();
      }, (_, command) => AssertSomeParameterReferencedMoreThanOnce(command));
    }

    [Test]
    public void ExplicitTvpAlgorithmReferencedMultipleTimesUsesDistinctParameterValues()
    {
      Require.AllFeaturesSupported(ProviderFeatures.TableValuedParameters);

      // Same dedup check via SQL reference count (see LargeRowFilter test above).
      var orders = new[] { 5, 7, 9 };
      ExecuteQuery(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Order.In(IncludeAlgorithm.TableValuedParameter, orders))
          .Where(split => split.Order.In(IncludeAlgorithm.TableValuedParameter, orders))
          .ToList();
      }, (_, command) => AssertSomeParameterReferencedMoreThanOnce(command));
    }

    [Test]
    public void BooleanClosureReferencedMultipleTimesIsNotBoundAsDbParameter()
    {
      // BooleanConstant bindings are expanded in SQL; the bool value must not appear in Parameters at all.
      var isActive = true;
      var boolParameterCount = 0;

      ExecuteQuery(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Active == isActive && split.Payment.Active == isActive)
          .Select(split => new {
            split.Id,
            Flag = split.Active == isActive,
            PaymentFlag = split.Payment.Active == isActive
          })
          .ToList();
      }, (_, command) => {
        foreach (DbParameter parameter in command.Parameters) {
          if (parameter.Value is bool) {
            boolParameterCount++;
          }
        }

        AssertEachParameterValueAppearsOnce(command);
      });

      Assert.That(boolParameterCount, Is.EqualTo(0));
    }

    [Test]
    public void DecimalClosureReferencedInFilterAndProjectionIsBoundOnce()
    {
      // `amountFilter` is referenced in two separate Where clauses compiled by distinct ExpressionProcessors.
      // Both use >= so they produce the same Regular binding type and are genuinely deduplicable.
      // (== would produce SmartNull bindings which are intentionally kept separate from Regular ones.)
      decimal amountFilter = 10m;
      AssertNoDuplicateParameterValues(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Amount >= amountFilter)
          .Where(split => split.Amount >= amountFilter)
          .ToList();
      }, parameter => parameter.Value is decimal decimalValue && decimalValue == amountFilter, minSqlReferences: 2);
    }

    [Test]
    public void TakeClosureReferencedInFilterAndPagingUsesDistinctParameterValues()
    {
      // `limit` is a closure in Where (Regular) and in Take (LimitOffset, inlined as a literal).
      var limit = 10;
      var limitParameterCount = 0;
      var literalOccurrences = 0;

      ExecuteQuery(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Order < limit)
          .Where(split => split.Order <= limit)
          .Take(limit)
          .ToList();
      }, (_, command) => {
        foreach (DbParameter parameter in command.Parameters) {
          if (parameter.Value is int intValue && intValue == limit) {
            limitParameterCount++;
          }
        }

        literalOccurrences = Regex.Matches(command.CommandText, $@"(?<!\w){limit}(?!\w)").Count;
        AssertEachParameterValueAppearsOnce(command);
      });

      Assert.That(limitParameterCount, Is.EqualTo(1));
      Assert.That(literalOccurrences, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void DistinctClosureValuesRemainSeparateParameters()
    {
      // Guard against over-deduplication: two different values must stay two parameters.
      var low = 100001;
      var high = 200002;
      ExecuteQuery(session => {
        _ = session.Query.All<PaymentSplit>()
          .Where(split => split.Order > low && split.Order < high)
          .ToList();
      }, (_, command) => {
        AssertEachParameterValueAppearsOnce(command);

        var parameters = command.Parameters.Cast<DbParameter>().ToArray();
        Assert.That(parameters.Count(p => p.Value is int value && value == low), Is.EqualTo(1));
        Assert.That(parameters.Count(p => p.Value is int value && value == high), Is.EqualTo(1));
      });
    }

    private int GetPaymentSplitTypeId() =>
      Domain.StorageNodeManager
        .GetNode(WellKnown.DefaultNodeId)
        .TypeIdRegistry[Domain.Model.Types[typeof(PaymentSplit)]];

    private void AssertNoDuplicateParameterValues(
      Action<Session> query,
      Func<DbParameter, bool> relevantParameter = null,
      int? minSqlReferences = null)
    {
      ExecuteQuery(query, (_, command) => {
        AssertEachParameterValueAppearsOnce(command);

        if (relevantParameter == null || minSqlReferences == null) {
          return;
        }

        var matchingParameters = command.Parameters.Cast<DbParameter>().Where(relevantParameter).ToArray();
        Assert.That(matchingParameters, Is.Not.Empty, "Expected at least one matching parameter.");

        var sqlReferenceCount = matchingParameters
          .Select(parameter => CountSqlReferences(command, parameter.ParameterName))
          .Sum();

        Assert.That(sqlReferenceCount, Is.GreaterThanOrEqualTo(minSqlReferences.Value),
          "Expected the bound parameter to be referenced multiple times in SQL.");
      });
    }

    private static void AssertEachParameterValueAppearsOnce(DbCommand command)
    {
      var duplicates = command.Parameters
        .Cast<DbParameter>()
        .Select(parameter => parameter.Value ?? DBNull.Value)
        .GroupBy(value => value, ParameterValueComparer.Instance)
        .Where(group => group.Count() > 1)
        .Select(group => $"{FormatParameterValue(group.Key)} x{group.Count()}")
        .ToArray();

      Assert.That(duplicates, Is.Empty,
        $"Each parameter value must be bound exactly once per query. Duplicates: {string.Join(", ", duplicates)}");
    }

    private static void AssertSomeParameterReferencedMoreThanOnce(DbCommand command)
    {
      var anyShared = command.Parameters
        .Cast<DbParameter>()
        .Any(p => CountSqlReferences(command, p.ParameterName) > 1);
      Assert.That(anyShared, Is.True,
        "Expected dedup: at least one parameter should be referenced more than once in SQL, "
        + $"but each of the {command.Parameters.Count} parameter(s) appears exactly once. "
        + "Command text: " + command.CommandText);
    }

    private static int CountSqlReferences(DbCommand command, string parameterName)
    {
      var reference = parameterName.StartsWith('@') ? parameterName : $"@{parameterName}";
      return Regex.Matches(command.CommandText, Regex.Escape(reference), RegexOptions.IgnoreCase).Count;
    }

    private static string FormatParameterValue(object value) =>
      value switch {
        DBNull => "<null>",
        IEnumerable enumerable and not string => $"[{string.Join(", ", enumerable.Cast<object>())}]",
        _ => value.ToString()
      };

    private void ExecuteQuery(Action<Session> query, Action<Session, DbCommand> inspectCommand)
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      void OnDbCommandExecuting(object sender, DbCommandEventArgs args)
      {
        inspectCommand(session, args.Command);
      }

      session.Events.DbCommandExecuting += OnDbCommandExecuting;
      try {
        query(session);
      }
      finally {
        session.Events.DbCommandExecuting -= OnDbCommandExecuting;
      }
    }

    private sealed class ParameterValueComparer : IEqualityComparer<object>
    {
      public static ParameterValueComparer Instance { get; } = new();

      public new bool Equals(object x, object y)
      {
        if (ReferenceEquals(x, y)) {
          return true;
        }

        if (x is null || y is null || x is DBNull || y is DBNull) {
          return false;
        }

        if (x.GetType() != y.GetType()) {
          return false;
        }

        if (x is IEnumerable enumerableX && x is not string) {
          return y is IEnumerable enumerableY
            && enumerableX.Cast<object>().SequenceEqual(enumerableY.Cast<object>());
        }

        return x.Equals(y);
      }

      public int GetHashCode(object obj)
      {
        if (obj is null || obj is DBNull) {
          return 0;
        }

        if (obj is IEnumerable enumerable && obj is not string) {
          var hash = new HashCode();
          foreach (var item in enumerable.Cast<object>()) {
            hash.Add(item);
          }

          return hash.ToHashCode();
        }

        return obj.GetHashCode();
      }
    }
  }
}
