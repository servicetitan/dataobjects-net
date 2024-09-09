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

namespace Xtensive.Sql.Drivers.SqlServer.v10
{
  internal class Driver : v09.Driver
  {
    protected override SqlCompiler CreateCompiler()
    {
      return new Compiler(this);
    }

    protected override Model.Extractor CreateExtractor()
    {
      return new Extractor(this);
    }

    protected override SqlTranslator CreateTranslator()
    {
      return new Translator(this);
    }

    protected override Sql.TypeMapper CreateTypeMapper()
    {
      return new TypeMapper(this);
    }

    protected override Info.ServerInfoProvider CreateServerInfoProvider()
    {
      return new ServerInfoProvider(this);
    }

    public override async Task CreateTypesIfNotExistAsync()
    {
      using var conn = CreateConnection();
      using var cmd = conn.CreateCommand("""
        IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '_DO_LongList') CREATE TYPE [_DO_LongList] AS TABLE ([Value] BIGINT NOT NULL PRIMARY KEY);
        IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '_DO_StringList') CREATE TYPE [_DO_StringList] AS TABLE ([Value] NVARCHAR(256) NOT NULL PRIMARY KEY);
        """);
      await cmd.ExecuteNonQueryAsync();
    }

    // As far as SqlGeometry and SqlGeography have no support in .Net Standard
    // these two methods are useless

    protected override void RegisterCustomMappings(TypeMappingRegistryBuilder builder)
    {
      base.RegisterCustomMappings(builder);
      builder.Add(typeof(List<>), null, builder.Mapper.BindTable, null);

      //      builder.Add(new GeometryMapper());
      //      builder.Add(new GeographyMapper());
    }

    //protected override void RegisterCustomReverseMappings(TypeMappingRegistryBuilder builder)
    //{
    //  base.RegisterCustomReverseMappings(builder);

    //  builder.AddReverse(CustomSqlType.Geometry, Type.GetType("Microsoft.SqlServer.Types.SqlGeometry, Microsoft.SqlServer.Types, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"));
    //  builder.AddReverse(CustomSqlType.Geography, Type.GetType("Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"));
    //}

    // Constructors

    public Driver(CoreServerInfo coreServerInfo, ErrorMessageParser errorMessageParser, bool checkConnectionIsAlive)
      : base(coreServerInfo, errorMessageParser, checkConnectionIsAlive)
    {
    }
  }
}
