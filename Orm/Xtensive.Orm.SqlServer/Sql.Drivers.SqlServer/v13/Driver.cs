// Copyright (C) 2009-2025 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.07.07

using Xtensive.Sql.Info;
using ISqlExecutor = Xtensive.Orm.Providers.ISqlExecutor;

namespace Xtensive.Sql.Drivers.SqlServer.v13
{
  internal class Driver : SqlServer.Driver
  {
    protected override Sql.Compiler.SqlCompiler CreateCompiler()
    {
      return new Compiler(this);
    }

    protected override Sql.Compiler.SqlTranslator CreateTranslator()
    {
      return new Translator(this);
    }

    protected override Info.ServerInfoProvider CreateServerInfoProvider()
    {
      return new ServerInfoProvider(this);
    }

    protected override Model.Extractor CreateExtractor()
    {
      return new Extractor(this);
    }

    protected override Sql.TypeMapper CreateTypeMapper()
    {
      return new TypeMapper(this);
    }

    public override Task CreateTypesIfNotExistAsync(ISqlExecutor executor) =>
      executor.ExecuteNonQueryAsync($"""
                                     IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '{TypeMapper.LongListTypeName}')
                                       CREATE TYPE [{TypeMapper.LongListTypeName}] AS TABLE ([Value] BIGINT NOT NULL PRIMARY KEY);
                                     IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '{TypeMapper.StringListTypeName}')
                                       CREATE TYPE [{TypeMapper.StringListTypeName}] AS TABLE ([Value] NVARCHAR(256) NOT NULL PRIMARY KEY);
                                     IF NOT EXISTS(SELECT 1 FROM sys.types WHERE name = '{TypeMapper.GuidListTypeName}')
                                       CREATE TYPE [{TypeMapper.GuidListTypeName}] AS TABLE ([Value] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);
                                     """);

    protected override void RegisterCustomMappings(TypeMappingRegistryBuilder builder)
    {
      base.RegisterCustomMappings(builder);
      builder.Add(typeof(List<long>), null, builder.Mapper.BindLongList, null);
      builder.Add(typeof(List<Guid>), null, builder.Mapper.BindGuidList, null);
      builder.Add(typeof(List<string>), null, builder.Mapper.BindStringList, null);
    }

    public Driver(CoreServerInfo coreServerInfo, ErrorMessageParser errorMessageParser, bool checkConnectionIsAlive)
      : base(coreServerInfo, errorMessageParser, checkConnectionIsAlive)
    {
    }
  }
}
