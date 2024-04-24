// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2009.05.25

using System.Linq.Expressions;

namespace Xtensive.Orm.Linq.Materialization;

internal readonly record struct MaterializationInfo(int EntitiesInRow, LambdaExpression Expression);
