// Copyright (C) 2008-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2008.09.26

using Xtensive.Sql;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Describes SQL parameter binding.
  /// </summary>
  public abstract class ParameterBinding
  {
    public TypeMapping TypeMapping { get; }

    /// <summary>
    /// Gets type of the parameter.
    /// Internally created <see cref="QueryParameterBinding"/>s
    /// may have this property set to <see langword="null"/>.
    /// Any user-created <see cref="QueryParameterBinding"/>
    /// always has this property set to non <see langword="null"/> value.
    /// </summary>
    public Type ValueType => TypeMapping?.Type;

    public ParameterTransmissionType TransmissionType { get; }

    /// <summary>
    /// Gets <see cref="SqlExpression"/> that allows
    /// to access parameter in SQL DOM query.
    /// </summary>
    public SqlExpression ParameterReference { get; }

    // Constructors

    protected ParameterBinding(TypeMapping typeMapping, ParameterTransmissionType transmissionType)
    {
      TypeMapping = typeMapping;
      TransmissionType = transmissionType;
      ParameterReference = SqlDml.Placeholder(this);
    }
  }
}
