// Copyright (C) 2011-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Malisa Ncube
// Created:    2011.04.

using System;
using System.Text;
using System.Linq;
using Xtensive.Core;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Ddl;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;
using Index = Xtensive.Sql.Model.Index;

namespace Xtensive.Sql.Drivers.Sqlite.v3
{
  internal class Translator : SqlTranslator
  {
    private const string DateTimeCastFormat = "%Y-%m-%d %H:%M:%f";

    /// <inheritdoc/>
    public override string DateTimeFormatString
    {
      get { return @"\'yyyy\-MM\-dd HH\:mm\:ss.fff\'"; }
    }

    public virtual string DateTimeOffsetFormatString
    {
      get { return @"\'yyyy\-MM\-dd HH\:mm\:ss.fffK\'"; }
    }

    public override string TimeSpanFormatString
    {
      get { return @"{0}{1}"; }
    }

    /// <inheritdoc/>
    public override string DdlStatementDelimiter
    {
      get { return ";"; }
    }

    /// <inheritdoc/>
    public override string BatchItemDelimiter
    {
      get { return ";\r\n"; }
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
      base.Initialize();
      FloatNumberFormat.NumberDecimalSeparator = ".";
      DoubleNumberFormat.NumberDecimalSeparator = ".";
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SchemaNode node)
    {
      context.Output.Append(QuoteIdentifier(node.DbName));
    }

    /// <inheritdoc/>
    public override string Translate(SqlFunctionType functionType)
    {
      switch (functionType) {
      case SqlFunctionType.Acos:
      case SqlFunctionType.Asin:
      case SqlFunctionType.Atan:
      case SqlFunctionType.Atan2:
      case SqlFunctionType.Sin:
      case SqlFunctionType.SessionUser:
      case SqlFunctionType.Sqrt:
      case SqlFunctionType.Square:
      case SqlFunctionType.Tan:
      case SqlFunctionType.Position:
      case SqlFunctionType.Power:
        throw SqlHelper.NotSupported(functionType.ToString());
      case SqlFunctionType.Concat:
        return "||";
      case SqlFunctionType.IntervalAbs:
        return "ABS";
      case SqlFunctionType.Substring:
        return "SUBSTR";
      case SqlFunctionType.IntervalNegate:
        return "-";
      case SqlFunctionType.CurrentDate:
        return "DATE()";
      case SqlFunctionType.BinaryLength:
        return "LENGTH";
      case SqlFunctionType.LastAutoGeneratedId:
        return "LAST_INSERT_ROWID()";
      case SqlFunctionType.DateTimeAddMonths:
        return "DATE";
      case SqlFunctionType.DateTimeConstruct:
        return "DATETIME";
      }
      return base.Translate(functionType);
    }

    public override string Translate(SqlCompilerContext context, object literalValue)
    {
      var literalType = literalValue.GetType();

      if (literalType==typeof (byte[]))
        return ByteArrayToString((byte[]) literalValue);
      if (literalType==typeof (TimeSpan))
        return Convert.ToString((long) ((TimeSpan) literalValue).Ticks * 100);
      if (literalType==typeof (Boolean))
        return ((Boolean) literalValue) ? "1" : "0";
      if (literalType==typeof (Guid))
        return ByteArrayToString(((Guid) literalValue).ToByteArray());
      if (literalType==typeof (DateTimeOffset))
        return ((DateTimeOffset) literalValue).ToString(DateTimeOffsetFormatString, DateTimeFormat);

      return base.Translate(context, literalValue);
    }

    private string ByteArrayToString(byte[] literalValue)
    {
      var result = new StringBuilder(literalValue.Length * 2 + 3);
      result.Append("x'");
      result.AppendHexArray(literalValue);
      result.Append("'");
      return result.ToString();
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlAlterTable node, AlterTableSection section)
    {
      switch (section) {
        case AlterTableSection.Entry:
          context.Output.Append("ALTER TABLE ");
          Translate(context, node.Table);
          break;
        case AlterTableSection.AddColumn:
          context.Output.Append("ADD");
          break;
        case AlterTableSection.Exit:
          break;
        default:
          throw SqlHelper.NotSupported(node.Action.GetType().Name);
      }
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, Constraint constraint, ConstraintSection section)
    {
      switch (section) {
        case ConstraintSection.Exit:
          if (constraint is ForeignKey fk) {
            if (fk.OnUpdate == ReferentialAction.Cascade) {
              context.Output.Append(") ON UPDATE CASCADE");
              return;
            }
            if (fk.OnDelete == ReferentialAction.Cascade) {
              context.Output.Append(") ON DELETE CASCADE");
              return;
            }
          }
          context.Output.Append(")");
          break;
        default:
          base.Translate(context, constraint, section);
          break;
      }
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SequenceDescriptor descriptor, SequenceDescriptorSection section)
    {
      //switch (section) {
      //  case SequenceDescriptorSection.Increment:
      //    if (descriptor.Increment.HasValue)
      //      return "AUTOINCREMENT";
      //    return string.Empty;
      //}
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlCreateTable node, CreateTableSection section)
    {
      var output = context.Output;
      switch (section) {
        case CreateTableSection.Entry:
          output.Append("CREATE ");
          if (node.Table is TemporaryTable temporaryTable) {
            output.Append("TEMPORARY TABLE ");
            Translate(context, temporaryTable);
          }
          else {
            output.Append("TABLE ");
            Translate(context, node.Table);
          }
          return;
        case CreateTableSection.Exit:
          return;
      }
      base.Translate(context, node, section);
    }

    public override void Translate(SqlCompilerContext context, SqlCreateView node, NodeSection section)
    {
      var output = context.Output;
      switch (section) {
        case NodeSection.Entry:
          if (node.View.ViewColumns.Count > 0) {
            output.Append(" (");
            bool first = true;
            foreach (DataTableColumn c in node.View.ViewColumns) {
              if (first)
                first = false;
              else
                output.Append(ColumnDelimiter);
              output.Append(c.DbName);
            }
            output.Append(")");
          }
          break;
        case NodeSection.Exit:
          break;
      }
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlDropSchema node)
    {
      throw SqlHelper.NotSupported(node.GetType().Name);
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlDropTable node)
    {
      context.Output.Append("DROP TABLE IF EXISTS ");
      Translate(context, node.Table);
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlDropView node)
    {
      context.Output.Append("DROP VIEW IF EXISTS ");
      Translate(context, node.View);
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlFunctionCall node, FunctionCallSection section, int position)
    {
      if (node.FunctionType == SqlFunctionType.LastAutoGeneratedId) {
        if (section == FunctionCallSection.Entry) {
          context.Output.Append(Translate(node.FunctionType));
          return;
        }
        if (section == FunctionCallSection.Exit) {
          return;
        }
      }
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

    public override void Translate(SqlCompilerContext context, SqlUpdate node, UpdateSection section)
    {
      context.Output.Append(section switch {
        UpdateSection.Entry => "UPDATE",
        UpdateSection.Set => "SET",
        UpdateSection.From => "FROM",
        UpdateSection.Where => "WHERE",
        _ => string.Empty
      });
    }

    public override void Translate(SqlCompilerContext context, SqlCreateIndex node, CreateIndexSection section)
    {
      Index index = node.Index;
      switch (section) {
        case CreateIndexSection.Entry:
          context.Output.Append($"CREATE {(index.IsUnique ? "UNIQUE" : String.Empty)} INDEX {QuoteIdentifier(index.Name)} ON {QuoteIdentifier(index.DataTable.Name)} ");
          return;
        case CreateIndexSection.Exit:
          return;
        default:
          base.Translate(context, node, section);
          break;
      }
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlDropIndex node)
    {
      context.Output.Append("DROP INDEX ")
        .Append(context == null ? node.Index.DataTable.Schema.Name : context.SqlNodeActualizer.Actualize(node.Index.DataTable.Schema))    //!!! why context can be null?
        .Append(".")
        .Append(QuoteIdentifier(node.Index.DbName));
    }

    /// <inheritdoc/>
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

    public override string Translate(SqlDateTimePart dateTimePart)
    {
      switch (dateTimePart) {
      case SqlDateTimePart.Year:
        return "'%Y'";
      case SqlDateTimePart.Month:
        return "'%m'";
      case SqlDateTimePart.Day:
        return "'%d'";
      case SqlDateTimePart.DayOfWeek:
        return "'%w'";
      case SqlDateTimePart.DayOfYear:
        return "'%j'";
      case SqlDateTimePart.Hour:
        return "'%H'";
      case SqlDateTimePart.Minute:
        return "'%M'";
      case SqlDateTimePart.Second:
        return "'%S'";
      default:
        throw SqlHelper.NotSupported(dateTimePart.ToString());
      }
    }

    public override string Translate(SqlIntervalPart intervalPart)
    {
      throw SqlHelper.NotSupported(intervalPart.ToString());
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlExtract extract, ExtractSection section)
    {
      context.Output.Append(section switch {
        ExtractSection.Entry => "CAST(STRFTIME(",
        ExtractSection.From => ", ",
        ExtractSection.Exit => ") as INTEGER)",
        _ => string.Empty
      });
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, SqlCast node, NodeSection section)
    {
      var output = context.Output;
      //http://www.sqlite.org/lang_expr.html
      var sqlType = node.Type.Type;

      if (sqlType == SqlType.DateTime ||
          sqlType == SqlType.DateTimeOffset) {
        switch (section) {
          case NodeSection.Entry:
            output.Append(string.Format("STRFTIME('{0}', ", DateTimeCastFormat));
            break;
          case NodeSection.Exit:
            output.Append(")");
            break;
          default:
            throw new ArgumentOutOfRangeException("section");
        }
      }
      else if (sqlType == SqlType.Binary ||
          sqlType == SqlType.Char ||
          sqlType == SqlType.Interval ||
          sqlType == SqlType.Int16 ||
          sqlType == SqlType.Int32 ||
          sqlType == SqlType.Int64) {
        switch (section) {
          case NodeSection.Entry:
            output.Append("CAST(");
            break;
          case NodeSection.Exit:
            output.Append("AS ").Append(Translate(node.Type)).Append(")");
            break;
          default:
            throw new ArgumentOutOfRangeException("section");
        }
      }
      else if (sqlType == SqlType.Decimal ||
          sqlType == SqlType.Double ||
          sqlType == SqlType.Float) {
        switch (section) {
          case NodeSection.Entry:
            break;
          case NodeSection.Exit:
            output.Append("+ 0.0");
            break;
          default:
            throw new ArgumentOutOfRangeException("section");
        }
      }
    }

    /// <inheritdoc/>
    public virtual string Translate(SqlCompilerContext context, SqlRenameColumn action)
    {
      throw SqlHelper.NotSupported(action.GetType().Name);
    }

    /// <inheritdoc/>
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
              output.Append("TRIM(");
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
          break;
        case TrimSection.Exit:
          switch (node.TrimType) {
            case SqlTrimType.Leading:
            case SqlTrimType.Trailing:
            case SqlTrimType.Both:
              output.Append(")");
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    /// <inheritdoc/>
    public override void Translate(SqlCompilerContext context, TableColumn column, TableColumnSection section)
    {
      switch (section) {
        case TableColumnSection.Type:
          if (column.SequenceDescriptor == null) {
            base.Translate(context, column, section);
          }
          else {
            context.Output.Append("integer");   // SQLite requires autoincrement columns to have exactly 'integer' type.
          }
          break;
        case TableColumnSection.Exit:
          if (column.SequenceDescriptor == null) {
            return;
          }
          var primaryKey = column.Table.TableConstraints.OfType<PrimaryKey>().FirstOrDefault();
          if (primaryKey == null) {
            return;
          }
          context.Output.Append("CONSTRAINT ")
            .Append(QuoteIdentifier(primaryKey.Name))
            .Append(" PRIMARY KEY AUTOINCREMENT");
          break;
        case TableColumnSection.GeneratedExit:
          break;
        default:
          base.Translate(context, column, section);
          break;
      }
    }

    /// <inheritdoc/>
    public override string Translate(Collation collation)
    {
      return collation.DbName;
    }

    /// <inheritdoc/>
    public override string Translate(SqlTrimType type)
    {
      return string.Empty;
    }

    /// <inheritdoc/>
    public override string Translate(SqlLockType lockType)
    {
      if (lockType.Supports(SqlLockType.Shared))
        return "SHARED";
      if (lockType.Supports(SqlLockType.Exclusive))
        return "EXCLUSIVE";
      if (lockType.Supports(SqlLockType.SkipLocked) || lockType.Supports(SqlLockType.ThrowIfLocked))
        return base.Translate(lockType);
      return "PENDING"; //http://www.sqlite.org/lockingv3.html Not sure whether this is the best alternative.
    }

    /// <inheritdoc/>
    public override string Translate(SqlNodeType type)
    {
      switch (type) {
      case SqlNodeType.DateTimePlusInterval:
      case SqlNodeType.DateTimeOffsetPlusInterval:
        return "+";
      case SqlNodeType.DateTimeMinusInterval:
      case SqlNodeType.DateTimeMinusDateTime:
      case SqlNodeType.DateTimeOffsetMinusInterval:
      case SqlNodeType.DateTimeOffsetMinusDateTimeOffset:
        return "-";
      case SqlNodeType.Overlaps:
        throw SqlHelper.NotSupported(type.ToString());
      default:
        return base.Translate(type);
      }
    }

    protected virtual string TranslateClrType(Type type)
    {
      switch (Type.GetTypeCode(type)) {
      case TypeCode.Boolean:
        return "bit";
      case TypeCode.Byte:
      case TypeCode.SByte:
      case TypeCode.Int16:
      case TypeCode.UInt16:
      case TypeCode.Int32:
      case TypeCode.UInt32:
        return "int";
      case TypeCode.Int64:
      case TypeCode.UInt64:
        return "bigint";
      case TypeCode.Decimal:
      case TypeCode.Single:
      case TypeCode.Double:
        return "numeric";
      case TypeCode.Char:
      case TypeCode.String:
        return "text";
      case TypeCode.DateTime:
        return "timestamp";
      default:
        if (type==typeof (TimeSpan))
          return "bigint";
        if (type==typeof (Guid))
          return "guid";
        return "text";
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
    }
  }
}
