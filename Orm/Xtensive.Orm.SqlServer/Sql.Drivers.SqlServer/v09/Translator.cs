// Copyright (C) 2003-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xtensive.Core;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Info;
using Xtensive.Sql.Model;
using Xtensive.Sql.Ddl;
using Xtensive.Sql.Dml;
using Xtensive.SqlServer.Resources;

namespace Xtensive.Sql.Drivers.SqlServer.v09
{
  internal class Translator : SqlTranslator
  {
    public override string DateTimeFormatString => @"'cast ('\'yyyy\-MM\-ddTHH\:mm\:ss\.fff\'' as datetime)'";
    public override string TimeSpanFormatString => string.Empty;

    public override void Initialize()
    {
      base.Initialize();
      FloatNumberFormat.NumberDecimalSeparator = ".";
      DoubleNumberFormat.NumberDecimalSeparator = ".";
    }

    public override void Translate(SqlCompilerContext context, SqlFunctionCall node, FunctionCallSection section, int position)
    {
      switch (section) {
        case FunctionCallSection.ArgumentEntry:
          break;
        case FunctionCallSection.ArgumentDelimiter:
          context.Output.Append(ArgumentDelimiter);
          break;
        default:
          base.Translate(context, node, section, position);
          break;
      }
    }

    public override string Translate(SqlFunctionType functionType) =>
      functionType switch {
        SqlFunctionType.IntervalAbs => "ABS",
        SqlFunctionType.IntervalNegate => "-",
        SqlFunctionType.CurrentDate => "GETDATE",
        SqlFunctionType.CharLength => "LEN",
        SqlFunctionType.BinaryLength => "DATALENGTH",
        SqlFunctionType.Position => "CHARINDEX",
        SqlFunctionType.Atan2 => "ATN2",
        SqlFunctionType.LastAutoGeneratedId => "SCOPE_IDENTITY",
        _ => base.Translate(functionType)
      };

    public override string Translate(SqlNodeType type)
    {
      switch (type) {
        case SqlNodeType.Count:
          return "COUNT_BIG";
        case SqlNodeType.Concat:
          return "+";
        case SqlNodeType.Overlaps:
          throw new NotSupportedException(string.Format(Strings.ExOperationXIsNotSupported, type));
      }
      return base.Translate(type);
    }

    public override void Translate(SqlCompilerContext context, TableColumn column, TableColumnSection section)
    {
      var output = context.Output;
      switch (section) {
        case TableColumnSection.Type:
          if (column.Domain == null) {
            output.Append(Translate(column.DataType));
          }
          else {
            TranslateIdentifier(output, column.Domain.Schema.DbName, column.Domain.DbName);
          }
          break;
        case TableColumnSection.GenerationExpressionEntry:
          output.Append("AS (");
          break;
        case TableColumnSection.GeneratedEntry:
        case TableColumnSection.GeneratedExit:
        case TableColumnSection.SetIdentityInfoElement:
        case TableColumnSection.Exit:
          break;
        default:
          base.Translate(context, column, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlAlterTable node, AlterTableSection section)
    {
      switch (section) {
        case AlterTableSection.AddColumn:
          context.Output.Append("ADD");
          break;
        case AlterTableSection.DropBehavior:
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SequenceDescriptor descriptor, SequenceDescriptorSection section)
    {
      TranslateIdentityDescriptor(context, descriptor, section);
    }

    protected void TranslateIdentityDescriptor(SqlCompilerContext context, SequenceDescriptor descriptor, SequenceDescriptorSection section)
    {
      switch (section) {
        case SequenceDescriptorSection.StartValue:
        case SequenceDescriptorSection.RestartValue:
          if (descriptor.StartValue.HasValue) {
            context.Output.Append("IDENTITY (").Append(descriptor.StartValue.Value).Append(RowItemDelimiter);
          }
          break;
        case SequenceDescriptorSection.Increment when descriptor.Increment.HasValue:
          context.Output.Append(descriptor.Increment.Value).Append(")");
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, Constraint constraint, ConstraintSection section)
    {
      var output = context.Output;
      switch (section) {
        case ConstraintSection.Unique:
          if (Driver.ServerInfo.UniqueConstraint.Features.Supports(UniqueConstraintFeatures.Clustered)) {
            output.Append(((UniqueConstraint) constraint).IsClustered ? "UNIQUE CLUSTERED (" : "UNIQUE NONCLUSTERED (");
          }
          else {
            output.Append("UNIQUE (");
          }
          break;
        case ConstraintSection.PrimaryKey:
          if (Driver.ServerInfo.PrimaryKey.Features.Supports(PrimaryKeyConstraintFeatures.Clustered)) {
            output.Append(((PrimaryKey) constraint).IsClustered ? "PRIMARY KEY CLUSTERED (" : "PRIMARY KEY NONCLUSTERED (");
          }
          else {
            output.Append("PRIMARY KEY (");
          }
          break;
        case ConstraintSection.Exit:
          if (constraint is ForeignKey fk) {
            output.Append(")");
            if (fk.OnUpdate != ReferentialAction.Restrict &&
              fk.OnUpdate != ReferentialAction.NoAction)
              output.Append(" ON UPDATE ").Append(Translate(fk.OnUpdate));
            if (fk.OnDelete != ReferentialAction.Restrict &&
              fk.OnDelete != ReferentialAction.NoAction)
              output.Append(" ON DELETE ").Append(Translate(fk.OnDelete));
          }
          else {
            output.Append(")");
          }
          break;
        default:
          base.Translate(context, constraint, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlCreateTable node, CreateTableSection section)
    {
      var output = context.Output;
      switch (section) {
        case CreateTableSection.Entry:
          output.Append("CREATE ");
          var temporaryTable = node.Table as TemporaryTable;
          if (temporaryTable != null) {
            if (temporaryTable.IsGlobal)
              temporaryTable.DbName = "##" + temporaryTable.Name;
            else
              temporaryTable.DbName = "#" + temporaryTable.Name;
          }
          output.Append("TABLE ");
          Translate(context, node.Table);
          return;
        case CreateTableSection.Exit:
          if (!string.IsNullOrEmpty(node.Table.Filegroup)) {
            output.Append(" ON ");
            TranslateIdentifier(output, node.Table.Filegroup);
          }
          return;
      }
      base.Translate(context, node, section);
    }

    public override void Translate(SqlCompilerContext context, SqlCreateView node, NodeSection section)
    {
      switch (section) {
        case NodeSection.Exit:
          if (node.View.CheckOptions == CheckOptions.Cascaded) {
            context.Output.Append("WITH CHECK OPTION");
          }
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlCreateDomain node, CreateDomainSection section)
    {
      var output = context.Output;
      switch (section) {
        case CreateDomainSection.Entry:
          output.Append("CREATE TYPE ");
          Translate(context, node.Domain);
          output.Append(" FROM ")
            .Append(Translate(node.Domain.DataType));
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlDropDomain node)
    {
      context.Output.Append("DROP TYPE ");
      Translate(context, node.Domain);
    }

    public override void Translate(SqlCompilerContext context, SqlAlterDomain node, AlterDomainSection section)
    {
      throw SqlHelper.NotSupported("ALTER DOMAIN"); // NOTE: Do not localize, it's an SQL keyword
    }

    public override void Translate(SqlCompilerContext context, SqlDeclareCursor node, DeclareCursorSection section)
    {
      if (section == DeclareCursorSection.Holdability || section == DeclareCursorSection.Returnability) {
        return;
      }
      base.Translate(context, node, section);
    }

    public override void Translate(SqlCompilerContext context, SqlJoinExpression node, JoinSection section)
    {
      var output = context.Output;
      switch (section) {
        case JoinSection.Specification:
          if (node.Expression == null)
            switch (node.JoinType) {
              case SqlJoinType.InnerJoin:
              case SqlJoinType.LeftOuterJoin:
              case SqlJoinType.RightOuterJoin:
              case SqlJoinType.FullOuterJoin:
                throw new NotSupportedException();
              case SqlJoinType.CrossApply:
                output.Append("CROSS APPLY");
                return;
              case SqlJoinType.LeftOuterApply:
                output.Append("OUTER APPLY");
                return;
            }
          var joinHint = TryFindJoinHint(context, node);
          output.Append(Translate(node.JoinType));
          if (joinHint != null) {
            output.Append(" ").Append(Translate(joinHint.Method));
          }
          output.Append(" JOIN");
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlQueryExpression node, QueryExpressionSection section)
    {
      if (node.All && section == QueryExpressionSection.All && (node.NodeType == SqlNodeType.Except || node.NodeType == SqlNodeType.Intersect))
        return;
      base.Translate(context, node, section);
    }

    private static SqlJoinHint TryFindJoinHint(SqlCompilerContext context, SqlJoinExpression node)
    {
      SqlQueryStatement statement = null;
      var traversalPath = context.GetTraversalPath();
      for (var i = traversalPath.Length; i-- > 0;) {
        if (traversalPath[i] is SqlQueryStatement sqlQueryStatement) {
          statement = sqlQueryStatement;
          break;
        }
      }
      if (statement == null || statement.Hints.Count == 0)
        return null;
      var candidate = statement.Hints
        .OfType<SqlJoinHint>()
        .FirstOrDefault(hint => hint.Table == node.Right);
      return candidate;
    }

    public override string Translate(SqlJoinMethod method)
    {
      switch (method) {
        case SqlJoinMethod.Hash:
          return "HASH";
        case SqlJoinMethod.Merge:
          return "MERGE";
        case SqlJoinMethod.Loop:
          return "LOOP";
        case SqlJoinMethod.Remote:
          return "REMOTE";
        default:
          return string.Empty;
      }
    }

    private static void AppendHint(IOutput output, string hint, ref bool hasHints)
    {
      if (hasHints) {
        output.Append(", ");
      }
      else {
        output.Append("OPTION (");
        hasHints = true;
      }
      output.Append(hint);
    }

    public override void Translate(SqlCompilerContext context, SqlSelect node, SelectSection section)
    {
      var output = context.Output;
      switch (section) {
        case SelectSection.Entry:
          base.Translate(context, node, section);
          break;
        case SelectSection.Limit:
          output.Append("TOP");
          break;
        case SelectSection.Offset:
          throw new NotSupportedException();
        case SelectSection.Exit:
          bool hasHints = false;
          foreach (var hint in node.Hints) {
            switch (hint) {
              case SqlForceJoinOrderHint:
                AppendHint(output, "FORCE ORDER", ref hasHints);
                break;
              case SqlFastFirstRowsHint sqlFastFirstRowsHint:
                AppendHint(output, "FAST ", ref hasHints);
                output.Append(sqlFastFirstRowsHint.Amount);
                break;
              case SqlNativeHint sqlNativeHint:
                AppendHint(output, sqlNativeHint.HintText, ref hasHints);
                break;
            }
          }
          if (hasHints) {
            output.Append(")");
          }
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlUpdate node, UpdateSection section)
    {
      switch (section) {
        case UpdateSection.Limit:
          context.Output.Append("TOP");
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlDelete node, DeleteSection section)
    {
      switch (section) {
        case DeleteSection.Entry:
          context.Output.Append("DELETE");
          break;
        case DeleteSection.Limit:
          context.Output.Append("TOP");
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlRenameTable node)
    {
      TranslateExecSpRename(context, node.Table, () => Translate(context, node.Table), node.NewName, null);
    }

    public override void Translate(SqlCompilerContext context, SqlCreateIndex node, CreateIndexSection section)
    {
      base.Translate(context, node, section);
      if (section != CreateIndexSection.Exit) {
        return;
      }
      var index = node.Index;
      var ftIndex = index as FullTextIndex;
      if (ftIndex == null) {
        return;
      }
      if (ftIndex.FullTextCatalog != null) {
        context.Output.Append(" ON ");
        TranslateIdentifier(context.Output, ftIndex.FullTextCatalog);
      }
      context.Output.Append(" WITH CHANGE_TRACKING ")
        .Append(TranslateChangeTrackingMode(ftIndex.ChangeTrackingMode));
    }

    public virtual void Translate(SqlCompilerContext context, SqlRenameColumn action)
    {
      var table = action.Column.Table;
      var schema = table.Schema;
      TranslateExecSpRename(context,
        table,
        () => TranslateIdentifier(context.Output, schema.Catalog.DbName, schema.DbName, table.DbName, action.Column.DbName),
        action.NewName,
        "COLUMN");
    }

    public virtual void Translate(SqlCompilerContext context, SqlAlterTable node, DefaultConstraint constraint)
    {
      TranslateExecDropDefaultConstraint(context, node, constraint);
    }

    protected void TranslateExecSpRename(SqlCompilerContext context, SchemaNode affectedNode, System.Action printObjectName, string newName, string type)
    {
      var output = context.Output;
      output.Append("EXEC ");
      if (context.HasOptions(SqlCompilerNamingOptions.DatabaseQualifiedObjects)) {
        TranslateIdentifier(output, affectedNode.Schema.Catalog.DbName);
        output.Append("..");
      }
      output.Append("sp_rename '");
      printObjectName();
      output.Append("', '")
        .Append(newName)
        .Append("'");
      if (type != null)
        output.Append($", '{type}'");
    }

    protected void TranslateExecDropDefaultConstraint(SqlCompilerContext context, SqlAlterTable node, DefaultConstraint defaultConstraint)
    {
      var column = defaultConstraint.Column;
      var table = defaultConstraint.Table;
      var schema = defaultConstraint.Column.DataTable.Schema;
      var gettingNameOfDefaultConstraintScript = GetCurentNameOfDefaultConstraintScript();

      var sqlVariableName = RemoveFromStringInvalidCharacters($"var_{schema.DbName}_{table.DbName}_{column.DbName}");

      context.Output.Append(string.Format(gettingNameOfDefaultConstraintScript,
          sqlVariableName,
          QuoteIdentifier(schema.Catalog.DbName),
          schema.DbName,
          table.DbName,
          column.DbName))
        .Append(" ")
        .Append("Exec(N'");
      Translate(context, node, AlterTableSection.Entry);
      Translate(context, node, AlterTableSection.DropConstraint);
      context.Output.Append(" CONSTRAINT[' + @")
        .Append(sqlVariableName)
        .Append(" + N']')");
    }

    protected virtual string GetCurentNameOfDefaultConstraintScript()
    {
      var resultBuilder = new StringBuilder();
      resultBuilder.Append("DECLARE @{0} VARCHAR(256) ");
      resultBuilder.Append(
      @"SELECT
        @{0} = {1}.sys.default_constraints.name
      FROM
        {1}.sys.all_columns
      INNER JOIN
        {1}.sys.tables
      ON all_columns.object_id = tables.object_id
      INNER JOIN
        {1}.sys.schemas
      ON tables.schema_id = schemas.schema_id
      INNER JOIN
        {1}.sys.default_constraints
      ON all_columns.default_object_id = default_constraints.object_id

      WHERE
        schemas.name = '{2}'
        AND tables.name = '{3}'
        AND all_columns.name = '{4}'");
      return resultBuilder.ToString();
    }

    protected string RemoveFromStringInvalidCharacters(string name)
    {
      var normalizedName = name.Aggregate(string.Empty,
        (current, character) => current.Insert(current.Length, !Char.IsLetterOrDigit(character)
          ? Convert.ToString('_', CultureInfo.InvariantCulture)
          : Convert.ToString(character, CultureInfo.InvariantCulture)));
      return normalizedName;
    }

    protected void AddUseStatement(SqlCompilerContext context, Catalog catalog)
    {
      if (context.HasOptions(SqlCompilerNamingOptions.DatabaseQualifiedObjects)) {
        context.Output.Append($"USE [{catalog.DbName}]; ");
      }
    }

    public override void Translate(SqlCompilerContext context, SqlExtract node, ExtractSection section)
    {
      switch (section) {
        case ExtractSection.Entry:
          context.Output.Append("DATEPART(");
          break;
        case ExtractSection.From:
          context.Output.Append(",");
          break;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    public override void Translate(SqlCompilerContext context, SqlTableRef node, TableSection section)
    {
      base.Translate(context, node, section);
      if (section != TableSection.AliasDeclaration)
        return;
      var select = context.GetTraversalPath()
        .OfType<SqlSelect>()
        .Where(s => s.Lock != SqlLockType.Empty)
        .FirstOrDefault();
      if (select != null) {
        context.Output.Append(" WITH (")
          .Append(Translate(select.Lock))
          .Append(")");
      }
    }

    public override void Translate(SqlCompilerContext context, SqlTrim node, TrimSection section)
    {
      var output = context.Output;
      switch (section) {
        case TrimSection.Entry:
          switch (node.TrimType) {
            case SqlTrimType.Leading:
              output.Append("LTRIM(");
              break;
            case SqlTrimType.Trailing:
              output.Append("RTRIM(");
              break;
            case SqlTrimType.Both:
              output.Append("LTRIM(RTRIM(");
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
          break;
        case TrimSection.Exit:
          switch (node.TrimType) {
            case SqlTrimType.Leading:
            case SqlTrimType.Trailing:
              output.Append(")");
              break;
            case SqlTrimType.Both:
              output.Append("))");
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public override void Translate(SqlCompilerContext context, SqlDropSchema node)
    {
      context.Output.Append("DROP SCHEMA ");
      TranslateIdentifier(context.Output, node.Schema.DbName);
    }

    public override void Translate(SqlCompilerContext context, SqlDropTable node)
    {
      context.Output.Append("DROP TABLE ");
      Translate(context, node.Table);
    }

    public override void Translate(SqlCompilerContext context, SqlDropView node)
    {
      context.Output.Append("DROP VIEW ");
      Translate(context, node.View);
    }

    public override string Translate(SqlTrimType type)
    {
      return string.Empty;
    }

    public override void Translate(SqlCompilerContext context, object literalValue)
    {
      var output = context.Output;
      switch (literalValue) {
        case char:
        case string:
          output.Append('N');
          TranslateString(output, literalValue.ToString());
          break;
        case TimeSpan v:
          output.Append(v.Ticks * 100);
          break;
        case bool v:
          output.Append(v ? "cast(1 as bit)" : "cast(0 as bit)");
          break;
        case DateTime dateTime:
          var dateTimeRange = (ValueRange<DateTime>) Driver.ServerInfo.DataTypes.DateTime.ValueRange;
          var newValue = ValueRangeValidator.Correct(dateTime, dateTimeRange);
          output.Append(newValue.ToString(DateTimeFormatString));
          break;
        case byte[] array:
          var builder = output.StringBuilder;
          builder.EnsureCapacity(builder.Length + 2 * (array.Length + 1));
          builder.Append("0x");
          builder.AppendHexArray(array);
          break;
        case Guid guid:
          TranslateString(output, guid.ToString());
          break;
        case Int64 v:
          output.Append($"CAST({v} as BIGINT)");
          break;
        default:
          base.Translate(context, literalValue);
          break;
      }
    }

    public override string Translate(SqlLockType lockType)
    {
      var items = new List<string>(3);
      items.Add("ROWLOCK");
      if (lockType.Supports(SqlLockType.Update))
        items.Add("UPDLOCK");
      else if (lockType.Supports(SqlLockType.Exclusive))
        items.Add("XLOCK");
      if (lockType.Supports(SqlLockType.ThrowIfLocked))
        items.Add("NOWAIT");
      else if (lockType.Supports(SqlLockType.SkipLocked))
        items.Add("READPAST");
      return items.ToCommaDelimitedString();
    }

    public override void Translate(IOutput output, Collation collation)
    {
      output.Append(collation.DbName);
    }

    protected virtual string TranslateChangeTrackingMode(ChangeTrackingMode mode)
    {
      switch (mode) {
        case ChangeTrackingMode.Auto:
          return "AUTO";
        case ChangeTrackingMode.Manual:
          return "MANUAL";
        case ChangeTrackingMode.Off:
          return "OFF";
        case ChangeTrackingMode.OffWithNoPopulation:
          return "OFF, NO POPULATION";
        default:
          return "AUTO";
      }
    }


    // Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Translator"/> class.
    /// </summary>
    /// <param name="driver">The driver.</param>
    protected internal Translator(SqlDriver driver)
      : base(driver)
    {
      FloatFormatString = "'cast('" + base.FloatFormatString + "'e0 as real')";
      DoubleFormatString = "'cast('" + base.DoubleFormatString + "'e0 as float')";
    }
  }
}