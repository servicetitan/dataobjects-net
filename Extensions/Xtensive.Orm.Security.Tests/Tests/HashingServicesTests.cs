// Copyright (C) 2011-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2011.05.30

using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Orm.Security.Cryptography;

namespace Xtensive.Orm.Security.Tests
{
  [TestFixture]
  public class HashingServicesTests : SecurityTestBase
  {
    private readonly List<string> values;

    [Test]
    public void PlainHashingServiceTest()
    {
      var service = new PlainHashingService();

      foreach (var value in values) {
        Assert.AreEqual(value, service.ComputeHash(value));
      }
    }

    [Test]
    public void MD5HashingServiceTest()
    {
      ExecuteTest(new MD5HashingService(), new MD5HashingService());
    }

    [Test]
    public void SHA1HashingServiceTest()
    {
      ExecuteTest(new SHA1HashingService(), new SHA1HashingService());
    }

    [Test]
    public void SHA256HashingServiceTest()
    {
      ExecuteTest(new SHA256HashingService(), new SHA256HashingService());
    }

    [Test]
    public void SHA384HashingServiceTest()
    {
      ExecuteTest(new SHA384HashingService(), new SHA384HashingService());
    }

    [Test]
    public void SHA512HashingServiceTest()
    {
      ExecuteTest(new SHA512HashingService(), new SHA512HashingService());
    }

    private void ExecuteTest(IHashingService service1, IHashingService service2)
    {
      foreach (var value in values) {
        var hash = service1.ComputeHash(value);
        Assert.IsNotEmpty(hash);
        Assert.IsTrue(service2.VerifyHash(value, hash));
        Assert.IsFalse(service2.VerifyHash(value, Convert.ToBase64String(new byte[] {33, 32,23,23,23,23,23,23,23,23,2,32,3,23,23,23})));
        Debug.WriteLine(service1.GetType().Name + "\t" + hash + "\t" + value);
      }
    }
    
    [Test]
    public void MD5HashingServiceVerifyTest()
    {
      var service = new MD5HashingService();
      Assert.IsTrue(service.VerifyHash("Branch", "N3HZr22YEGQ6G1VbNAr55HL7SCEXdXLS"));
      Assert.IsTrue(service.VerifyHash("<>c", "J2iEAP5TCSCiGnzViGbIkLfYPJxGwkZF"));
    }
    
    [Test]
    public void SHA1HashingServiceVerifyTest()
    {
      var service = new SHA1HashingService();
      Assert.IsTrue(service.VerifyHash("Branch", "LcOPOQxgJXnVyUlQPlCWPg4sAuuO/WXECUtnbQ=="));
      Assert.IsTrue(service.VerifyHash("<>c", "b+MXXLPgB05uq+ZqoNfhpkIIvJ2dUhbmvtUQkA=="));
    }

    [Test]
    public void SHA256HashingServiceVerifyTest()
    {
      var service = new SHA256HashingService();
      Assert.IsTrue(service.VerifyHash("Branch", "BsXx3mNFIUBBc0LTdNwM+nydddpt8O6WjJsVW5RPhtsJdk86ZW9fuw=="));
      Assert.IsTrue(service.VerifyHash("<>c", "vTTce9tjdUdpkgoPTTbkIGwO+HZlpZ6obsjB1wI2FXf70pLqn9x9Fw=="));
    }

    [Test]
    public void SHA384HashingServiceVerifyTest()
    {
      var service = new SHA384HashingService();
      Assert.IsTrue(service.VerifyHash("Branch", "azbF4/JRMxyepJGK7IpRGRl8ZulViQx1LH5c0GAcdmyAeSPpHzlRkJ8R9Hc/+8V27kIhBzxZL3I="));
      Assert.IsTrue(service.VerifyHash("<>c", "jvXwho+E13ky+CjKAy0s+dbZfrFkyebqwBjfnjtKo3EyZwpMMgxhCZNJKXA1nAnKW0QfTCVKOcg="));
    }

    [Test]
    public void SHA512HashingServiceVerifyTest()
    {
      var service = new SHA512HashingService();
      Assert.IsTrue(service.VerifyHash("Branch", "fpfq1UvTTSeXdn/wkr4A7GS61tndvCcpqLKHYIzhcPiGHGpInk+VQajZnwzJ6PiexNjJOFMGSfTYijxycE6RVFdxVL23GGja"));
      Assert.IsTrue(service.VerifyHash("<>c", "Swf/Cv0n0Fs0mjPfh+8TA5cKdBWUQbvxHbH3nrHpuWKxjFN3H44IwmJAJkUvSnDtLdl6xwAigmhTU4FH9NVBEbsjgc7QjTUv"));
    }

    [Test]
    public void InitializationTest()
    {
      var s = Domain.Services.Get<IHashingService>("md5");
      Assert.IsNotNull(s);
      Assert.IsInstanceOf<MD5HashingService>(s);

      s = Domain.Services.Get<IHashingService>("sha1");
      Assert.IsNotNull(s);
      Assert.IsInstanceOf<SHA1HashingService>(s);

      s = Domain.Services.Get<IHashingService>("sha256");
      Assert.IsNotNull(s);
      Assert.IsInstanceOf<SHA256HashingService>(s);

      s = Domain.Services.Get<IHashingService>("sha384");
      Assert.IsNotNull(s);
      Assert.IsInstanceOf<SHA384HashingService>(s);

      s = Domain.Services.Get<IHashingService>("sha512");
      Assert.IsNotNull(s);
      Assert.IsInstanceOf<SHA512HashingService>(s);

      s = Domain.Services.Get<IHashingService>("plain");
      Assert.IsNotNull(s);
      Assert.IsInstanceOf<PlainHashingService>(s);
    }

    public HashingServicesTests()
    {
      values = Assembly.GetExecutingAssembly().GetTypes().Select(t => t.FullName).ToList();
    }
  }
}
