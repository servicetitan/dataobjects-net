// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Ilyin
// Created:    2007.07.18

using NUnit.Framework;

namespace Xtensive.Orm.Tests.Core;

[TestFixture]
public class UrlInfoTest
{
  [Test]
  public void CombinedTest()
  {
    UrlInfo a1 = UrlInfo.Parse("tcp://user:password@someHost:1000/someUrl/someUrl?someParameter=someValue&someParameter2=someValue2");
    UrlInfo a2 = UrlInfo.Parse("tcp://user:password@someHost:1000/someUrl/someUrl?someParameter=someValue&someParameter2=someValue2");
    UrlInfo aX = UrlInfo.Parse("tcp://user:password@someHost:1000/someUrl/someUrl?someParameter2=someValue2&someParameter=someValue");
    UrlInfo b  = UrlInfo.Parse("tcp://user:password@someHost:1000/someUrl/someUrl");

    Assert.IsTrue(a1.GetHashCode()==a2.GetHashCode());
    Assert.IsTrue(a1.GetHashCode()!=b.GetHashCode());

    Assert.IsTrue(a1.Equals(a2));
    Assert.IsFalse(a1.Equals(b));
  }

  [Test]
  public void WithTest()
  {
    UrlInfo a1 = UrlInfo.Parse("tcp://user:password@someHost:1000/someUrl/someUrl?p3=v3&p4=v4&p1=v1&p2=v2");
    var a2 = a1 with {
      Port = 2000,
      Password = "xxx",
      Protocol = "unkProto",
      Resource = "other/resource",
      Params = new Dictionary<string, string> { { "a", "b" } }
    };
    Assert.AreEqual("unkProto://user:xxx@someHost:2000/other/resource?a=b", a2.ToString());
    Assert.IsTrue(a2.Equals(a2));
    Assert.IsFalse(a1.Equals(a2));
    var a3 = UrlInfo.Parse("unkProto://user:xxx@someHost:2000/other/resource?a=b");
    Assert.IsTrue(a2 == a3);
  }

  [Test]
  public void TestUrlProps()
  {
    var url = UrlInfo.Parse("sqlserver://int:xxx@127.0.0.1:51571/db");
    Assert.AreEqual("sqlserver", url.Protocol);
    Assert.AreEqual("int", url.User);
    Assert.AreEqual("xxx", url.Password);
    Assert.AreEqual("127.0.0.1", url.Host);
    Assert.AreEqual(51571, url.Port);
    Assert.AreEqual("db", url.Resource);
  }
}
