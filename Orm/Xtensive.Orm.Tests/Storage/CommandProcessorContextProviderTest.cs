// Copyright (C) 2019-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2019.07.12

using NUnit.Framework;

namespace Xtensive.Orm.Tests.Storage
{
  public class CommandProcessorContextProviderTest : AutoBuildTest
  {
    protected override void PopulateData()
    {
      _ = CreateSessionAndTransaction();
    }

    [Test]
    public void GetContextTest()
    {
      var session = Session.Demand();
      var context = session.CommandProcessorContextProvider.ProvideContext();

      Assert.That(context, Is.Not.Null);
      Assert.That(context.AllowPartialExecution, Is.False);
      Assert.That(context.ActiveCommand, Is.Null);
      Assert.That(context.ActiveTasks, Is.Not.Null);
      Assert.That(context.ActiveTasks.Count, Is.EqualTo(0));
      Assert.That(context.ProcessingTasks, Is.Not.Null);
      Assert.That(context.ProcessingTasks.Count, Is.EqualTo(0));

      Assert.That(session.CommandProcessorContextProvider.ProvideContext(), Is.Not.SameAs(session.CommandProcessorContextProvider.ProvideContext()));
    }

    [Test]
    public void GetContextForPartialExecutionTest()
    {
      var session = Session.Demand();
      var context = session.CommandProcessorContextProvider.ProvideContext(true);

      Assert.That(context, Is.Not.Null);
      Assert.That(context.AllowPartialExecution, Is.True);
      Assert.That(context.ActiveCommand, Is.Null);
      Assert.That(context.ActiveTasks, Is.Not.Null);
      Assert.That(context.ActiveTasks.Count, Is.EqualTo(0));
      Assert.That(context.ProcessingTasks, Is.Not.Null);
      Assert.That(context.ProcessingTasks.Count, Is.EqualTo(0));

      Assert.That(session.CommandProcessorContextProvider.ProvideContext(true), Is.Not.SameAs(session.CommandProcessorContextProvider.ProvideContext(true)));
    }
  }
}
