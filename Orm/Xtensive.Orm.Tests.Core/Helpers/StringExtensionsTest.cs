// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2009.03.17

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xtensive.Core;

namespace Xtensive.Orm.Tests.Core.Helpers
{
  [TestFixture]
  public class StringExtensionsTest
  {
    [TestCase(new[] { "" }, '\\', ',', ExpectedResult = "")]
    [TestCase(new[] { "A" }, '\\', ',', ExpectedResult = "A")]
    [TestCase(new[] { "A", "B" }, '\\', ',', ExpectedResult = "A,B")]
    [TestCase(new[] { "A", "B", "C" }, '\\', ',', ExpectedResult = "A,B,C")]
    [TestCase(new[] { "," }, '\\', ',', ExpectedResult = "\\,")]
    [TestCase(new[] { "\\" }, '\\', ',', ExpectedResult = "\\\\")]
    [TestCase(new[] { ",", "" }, '\\', ',', ExpectedResult = "\\,,")]
    [TestCase(new[] { "", "," }, '\\', ',', ExpectedResult = ",\\,")]
    [TestCase(new[] { "", "" }, '\\', ',', ExpectedResult = ",")]
    public string RevertibleJoinTest(IEnumerable<string> source, char escape, char delimiter)
    {
      return source.RevertibleJoin(escape, delimiter);
    }

    [TestCase("", '\\', ',', ExpectedResult = new[] { "" })]
    [TestCase("A", '\\', ',', ExpectedResult = new[] { "A" })]
    [TestCase("A,B", '\\', ',', ExpectedResult = new[] { "A", "B" })]
    [TestCase("A,B,C", '\\', ',', ExpectedResult = new[] { "A", "B", "C" })]
    [TestCase("\\", '\\', ',', ExpectedResult = new[] { "" })]
    [TestCase("\\,", '\\', ',', ExpectedResult = new[] { "," })]
    [TestCase("\\\\", '\\', ',', ExpectedResult = new[] { "\\" })]
    [TestCase("\\,,", '\\', ',', ExpectedResult = new[] { ",", "" })]
    [TestCase(",\\,", '\\', ',', ExpectedResult = new[] { "", "," })]
    [TestCase(",", '\\', ',', ExpectedResult = new[] { "", "" })]
    [TestCase("A,\\,", '\\', ',', ExpectedResult = new[] { "A", "," })]
    [TestCase("\\A", '\\', ',', ExpectedResult = new[] { "A" })]
    [TestCase("A\\", '\\', ',', ExpectedResult = new[] { "A" })]
    [TestCase("A,", '\\', ',', ExpectedResult = new[] { "A", "" })]
    [TestCase("\\A\\B", '\\', ',', ExpectedResult = new[] { "AB" })]
    [TestCase("\\A\\B\\", '\\', ',', ExpectedResult = new[] { "AB" })]
    public IEnumerable<string> RevertibleSplitTest(string source, char escape, char delimiter)
    {
      return source.RevertibleSplit(escape, delimiter);
    }

    [TestCase("", '\\', ',', ExpectedResult = new[] { "", null })]
    [TestCase("A", '\\', ',', ExpectedResult = new[] { "A", null })]
    [TestCase("A,B", '\\', ',', ExpectedResult = new[] { "A", "B" })]
    [TestCase("A,B,C", '\\', ',', ExpectedResult = new[] { "A", "B,C" })]
    [TestCase("\\", '\\', ',', ExpectedResult = new[] { "", null })]
    [TestCase("\\,", '\\', ',', ExpectedResult = new[] { ",", null })]
    [TestCase("\\\\", '\\', ',', ExpectedResult = new[] { "\\", null })]
    [TestCase("\\,,", '\\', ',', ExpectedResult = new[] { ",", "" })]
    [TestCase(",\\,", '\\', ',', ExpectedResult = new[] { "", "\\," })]
    [TestCase(",", '\\', ',', ExpectedResult = new[] { "", "" })]
    [TestCase("A,\\,", '\\', ',', ExpectedResult = new[] { "A", "\\," })]
    [TestCase("\\A", '\\', ',', ExpectedResult = new[] { "A", null })]
    [TestCase("A\\", '\\', ',', ExpectedResult = new[] { "A", null })]
    [TestCase("A,", '\\', ',', ExpectedResult = new[] { "A", "" })]
    [TestCase("\\A\\B", '\\', ',', ExpectedResult = new[] { "AB", null })]
    [TestCase("\\A\\B\\", '\\', ',', ExpectedResult = new[] { "AB", null })]
    public string[] RevertibleSplitFirstAndTailTest(string source, char escape, char delimiter)
    {
      var result = source.RevertibleSplitFirstAndTail(escape, delimiter);
      return [result.First, result.Second];
    }

    [TestCase("A", 0, false, ExpectedResult = "A")]
    [TestCase("A", 1, false, ExpectedResult = "A")]
    [TestCase("A", 2, false, ExpectedResult = "A")]
    [TestCase("A", 0, true, ExpectedResult = "A")]
    [TestCase("A", 1, true, ExpectedResult = " A")]
    [TestCase("A", 2, true, ExpectedResult = "  A")]

    [TestCase("A\r\nB", 1, false, ExpectedResult = "A\r\n B")]
    [TestCase("A\r\nB", 1, true, ExpectedResult = " A\r\n B")]
    public string IndentTest(string source, int amount, bool firstLine)
    {
      return source.Indent(amount, firstLine);
    }

    [TestCase("uewryewsf", "%sf", ExpectedResult = true)]
    [TestCase("s__asdf", "_%asdf", ExpectedResult = true)]
    [TestCase("dsfEEEE", "dsf%", ExpectedResult = true)]
    [TestCase("Afigdf", "_figdf", ExpectedResult = true)]
    [TestCase("fsdfASDsdfs", "fsdf___sdfs", ExpectedResult = true)]
    [TestCase("my name is Alex.", "my name is _____", ExpectedResult = true)]
    [TestCase("how old are you?", "how old % you_", ExpectedResult = true)]
    [TestCase("hi, I'm alex. I'm 26", "hi, I'm ____. I'm %", ExpectedResult = true)]
    [TestCase("it's another test string%%%", "it's another test string!%!%!%", ExpectedResult = false)]
    [TestCase("it's another test string%%%", "it's another test string!%!%!%", '!', ExpectedResult = true)]
    [TestCase("string with error.", "String with error_", ExpectedResult = false)]
    [TestCase("Another string with error.", "another string with err%.", ExpectedResult = false)]
    public bool LikeExtensionTest(string source, string what, char escape = ' ')
    {
      if (escape == ' ')
        return source.Like(what);
      return source.Like(what, escape);
    }

    [TestCase("aRRRRa%", "a%a%%", '%')]
    public void LikeExtensionTestThrows(string source, string what, char escape = ' ')
    {
      _ = Assert.Throws<ArgumentException>(() => source.Like(what, escape));
    }

    [TestCase("(abc)", ExpectedResult = "abc")]
    [TestCase("abc", ExpectedResult = "abc")]
    [TestCase("((((Convert(abc)))))", ExpectedResult = "Convert(abc)")]
    [TestCase("(Convert(abc))", ExpectedResult = "Convert(abc)")]
    [TestCase("((((abc))", ExpectedResult = "((abc")]
    [TestCase("((abc))))", ExpectedResult = "abc))")]
    public string TrimRoundBracketsSymmetricallyTest(string source)
    {
      return source.StripRoundBrackets();
    }
  }
}