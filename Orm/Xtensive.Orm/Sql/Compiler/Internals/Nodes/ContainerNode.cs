// Copyright (C) 2003-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xtensive.Sql.Compiler
{
  public interface IContainerNodeBuilder
  {
    IContainerNodeBuilder Append(string text);
    IContainerNodeBuilder Append(long v);
    void AppendPlaceholder(PlaceholderNode placeholder);    
  }

  internal static class ContainerNodeBuilderExtensions
  {
    public static void AppendPlaceholderWithId(this IContainerNodeBuilder builder, object id) => builder.AppendPlaceholder(new PlaceholderNode(id));
  }

  /// <summary>
  /// Container node in SQL DOM query model.
  /// </summary>
  public class ContainerNode : Node, IContainerNodeBuilder, IEnumerable<Node>
  {
    private readonly StringBuilder stringBuilder = new StringBuilder();
    private readonly List<Node> children = new List<Node>();

    public IEnumerable<Node> Children
    {
      get {
        FlushBuffer();
        return children;
      }
    }

    public IEnumerator<Node> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool RequireIndent;

    public Node Current => Children.Last();
    public bool IsEmpty => !Children.Any();

    internal override void AcceptVisitor(NodeVisitor visitor)
    {
      visitor.Visit(this);
    }

    public void AppendCycleItem(int index)
    {
      Add(new CycleItemNode(index));
    }

    private void FlushBuffer()
    {
      if (stringBuilder.Length > 0) {
        children.Add(new TextNode(stringBuilder.ToString()));
        stringBuilder.Clear();
      }
    }

    public void Add(Node node)
    {
      FlushBuffer();
      children.Add(node);
    }

    void IContainerNodeBuilder.AppendPlaceholder(PlaceholderNode placeholder) =>
      Add(placeholder);
    
    public IContainerNodeBuilder Append(string text)
    {
      if (!string.IsNullOrEmpty(text)) {
        stringBuilder.Append(text);
      }
      return this;
    }

    public IContainerNodeBuilder Append(long v)
    {
      stringBuilder.Append(v);
      return this;
    }

    public void AppendDelimiter(string text)
    {
      Add(new DelimiterNode(SqlDelimiterType.Row, text));
    }

    public void AppendDelimiter(string text, SqlDelimiterType type)
    {
      Add(new DelimiterNode(type, text));
    }

    // Constructors

    public ContainerNode(bool requireIndent)
    {
      RequireIndent = requireIndent;
    }

    public ContainerNode()
    {
      RequireIndent = false;
    }
  }
}