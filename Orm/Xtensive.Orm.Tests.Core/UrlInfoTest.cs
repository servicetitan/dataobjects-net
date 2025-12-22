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

    Assert.That(a1.GetHashCode()==a2.GetHashCode(), Is.True);
    Assert.That(a1.GetHashCode()!=b.GetHashCode(), Is.True);

    Assert.That(a1.Equals(a2), Is.True);
    Assert.That(a1.Equals(b), Is.False);
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
    Assert.That("unkProto://user:xxx@someHost:2000/other/resource?a=b", Is.EqualTo(a2.ToString()));
    Assert.That(a2.Equals(a2), Is.True);
    Assert.That(a1.Equals(a2), Is.False);
    var a3 = UrlInfo.Parse("unkProto://user:xxx@someHost:2000/other/resource?a=b");
    Assert.That(a2 == a3, Is.True);
  }

  [Test]
  public void TestUrlProps()
  {
    var url = UrlInfo.Parse("sqlserver://int:xxx@127.0.0.1:51571/db");
    Assert.That(url.Protocol, Is.EqualTo("sqlserver"));
    Assert.That(url.User, Is.EqualTo("int"));
    Assert.That(url.Password, Is.EqualTo("xxx"));
    Assert.That(url.Host, Is.EqualTo("127.0.0.1"));
    Assert.That(url.Port, Is.EqualTo(51571));
    Assert.That(url.Resource, Is.EqualTo("db"));
  }
}
