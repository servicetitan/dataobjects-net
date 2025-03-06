// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Ilyin
// Created:    2007.07.18

using NUnit.Framework;

namespace Xtensive.Orm.Tests.Core
{
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
      var a2 = a1 with { Port = 2000 };
      Assert.AreEqual("tcp://user:password@someHost:2000/someUrl/someUrl?p1=v1&p2=v2&p3=v3&p4=v4", a2.ToString());
      Assert.IsTrue(a2.Equals(a2));
      Assert.IsFalse(a1.Equals(a2));
      var a3 = UrlInfo.Parse("tcp://user:password@someHost:2000/someUrl/someUrl?p1=v1&p2=v2&p3=v3&p4=v4");
      Assert.IsTrue(a2 == a3);
    }
  }
}
