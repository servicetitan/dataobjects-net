using NUnit.Framework;
using Xtensive.Orm.Tests.Issues.IssueGithub0114_QueryRootReuseCauseNoRefJoinModel;

namespace Xtensive.Orm.Tests.Issues;

[TestFixture]
public sealed partial class IssueGithub0114_QueryRootReuseCauseNoRefJoin : AutoBuildTest
{
  [Test]
  public void InnerWhere()
  {
    using var session = Domain.OpenSession();
    using var tx = session.OpenTransaction();
    _ = session.Query.All<Promotion>().GroupBy(p => p.Id).Select(g =>
      g.Where(x => x.CampainName == null).Count()
    ).ToArray();
  }

  [Test]
  public void InnerCount()
  {
    using var session = Domain.OpenSession();
    using var tx = session.OpenTransaction();
    _ = session.Query.All<Promotion>().GroupBy(p => p.Id).Select(g =>
      g.Count(x => x.CampainName == null)
    ).ToArray();
  }
}

