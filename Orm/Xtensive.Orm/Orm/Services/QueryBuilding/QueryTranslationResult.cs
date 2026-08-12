// Copyright (C) 2012-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.02.27

using Xtensive.Sql.Dml;

namespace Xtensive.Orm.Services;

public readonly record struct QueryTranslationResult(SqlSelect Query, IReadOnlyList<QueryParameterBinding> ParameterBindings);
