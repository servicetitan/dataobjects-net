// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl
{
  [Serializable]
  public class SqlDropTranslation : SqlStatement, ISqlCompileUnit
  {
    public Translation Translation { get; }

    internal override SqlDropTranslation Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.Translation));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    internal SqlDropTranslation(Translation translation) : base(SqlNodeType.Drop)
    {
      Translation = translation;
    }
  }
}
