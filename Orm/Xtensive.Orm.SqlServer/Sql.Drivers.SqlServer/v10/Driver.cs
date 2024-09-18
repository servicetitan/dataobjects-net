// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.07.07

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Info;
using ISqlExecutor = Xtensive.Orm.Providers.ISqlExecutor;

namespace Xtensive.Sql.Drivers.SqlServer.v10;

internal class Driver(CoreServerInfo coreServerInfo, ErrorMessageParser errorMessageParser, bool checkConnectionIsAlive)
  : v09.Driver(coreServerInfo, errorMessageParser, checkConnectionIsAlive)
{
  protected override SqlCompiler CreateCompiler() => new Compiler(this);
  protected override Model.Extractor CreateExtractor() => new Extractor(this);
  protected override SqlTranslator CreateTranslator() => new Translator(this);
  protected override Sql.TypeMapper CreateTypeMapper() => new TypeMapper(this);
  protected override Info.ServerInfoProvider CreateServerInfoProvider() => new ServerInfoProvider(this);

  public override Task CreateTypesIfNotExistAsync(ISqlExecutor executor) =>
    executor.ExecuteNonQueryAsync($"""
      IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '{TypeMapper.LongListTypeName}')
        CREATE TYPE [{TypeMapper.LongListTypeName}] AS TABLE ([Value] BIGINT NOT NULL PRIMARY KEY);
      IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '{TypeMapper.StringListTypeName}')
        CREATE TYPE [{TypeMapper.StringListTypeName}] AS TABLE ([Value] NVARCHAR(256) NOT NULL PRIMARY KEY);
      """);

  protected override void RegisterCustomMappings(TypeMappingRegistryBuilder builder)
  {
    base.RegisterCustomMappings(builder);
    builder.Add(typeof(List<long>), null, builder.Mapper.BindLongList, null);
    builder.Add(typeof(List<string>), null, builder.Mapper.BindStringList, null);

    // As far as SqlGeometry and SqlGeography have no support in .Net Standard
    // these two methods are useless
    //      builder.Add(new GeometryMapper());
    //      builder.Add(new GeographyMapper());
  }

  //protected override void RegisterCustomReverseMappings(TypeMappingRegistryBuilder builder)
  //{
  //  base.RegisterCustomReverseMappings(builder);

  //  builder.AddReverse(CustomSqlType.Geometry, Type.GetType("Microsoft.SqlServer.Types.SqlGeometry, Microsoft.SqlServer.Types, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"));
  //  builder.AddReverse(CustomSqlType.Geography, Type.GetType("Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"));
  //}
}
