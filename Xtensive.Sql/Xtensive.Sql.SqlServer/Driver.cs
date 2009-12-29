// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.06.23

using Xtensive.Sql.Info;

namespace Xtensive.Sql.SqlServer
{
  internal abstract class Driver : SqlDriver
  {
    public override SqlConnection CreateConnection()
    {
      return new Connection(this);
    }

    // Constructors

    protected Driver(CoreServerInfo coreServerInfo)
      : base(coreServerInfo)
    {
    }
  }
}