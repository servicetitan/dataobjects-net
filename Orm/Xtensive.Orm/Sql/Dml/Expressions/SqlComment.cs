// Copyright (C) 2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Edgar Isajanyan
// Created:    2021.09.13

using System;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlComment : SqlExpression
  {
    /// <summary>
    /// Gets the value.
    /// </summary>
    public string Text { get; private set; }
    
    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlComment>(expression);
      Text = replacingExpression.Text;
    }

    internal override SqlComment Clone(SqlNodeCloneContext context) =>
      context.GetOrAdd(this, static (t, c) => new(t.Text));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    public static SqlComment Join(SqlComment comment1, SqlComment comment2)
    {
      if (ReferenceEquals(comment1, comment2))
        return comment1;

      if (comment1 is null && comment2 is null)
        return null;
      if (comment1 is not null) {
        if (comment2 is not null && !ContainsAsToken(comment1.Text, comment2.Text))
          comment1.Text += $" {comment2.Text}";
        return comment1;
      }
      else
        return comment2;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="needle"/> already appears
    /// in <paramref name="haystack"/> as a whole token bounded by start/end of the
    /// string or by the single-space separator that <see cref="Join"/> emits between
    /// joined comments. Used by <see cref="Join"/> to avoid appending the same tag
    /// text more than once when the same tagged source is reached by the SQL
    /// compiler through multiple paths (e.g. via cloned subqueries that share an
    /// aliased <see cref="SqlComment"/> reference produced by
    /// <c>SqlSelect.ShallowClone</c>).
    /// </summary>
    private static bool ContainsAsToken(string haystack, string needle)
    {
      if (string.IsNullOrEmpty(needle)) {
        return true;
      }
      if (string.IsNullOrEmpty(haystack)) {
        return false;
      }
      var needleLength = needle.Length;
      var haystackLength = haystack.Length;
      var index = 0;
      while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0) {
        var leftOk = index == 0 || haystack[index - 1] == ' ';
        var end = index + needleLength;
        var rightOk = end == haystackLength || haystack[end] == ' ';
        if (leftOk && rightOk) {
          return true;
        }
        index = end;
      }
      return false;
    }

    // Constructors

    public SqlComment(string text)
      : base(SqlNodeType.Comment)
    {
      Text = text;
    }
  }
}
