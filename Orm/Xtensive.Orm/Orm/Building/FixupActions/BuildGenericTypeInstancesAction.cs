// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.05.28

using Xtensive.Orm.Building.Definitions;

namespace Xtensive.Orm.Building.FixupActions
{
  internal sealed class BuildGenericTypeInstancesAction : TypeAction
  {
    public override void Run(FixupActionProcessor processor)
    {
      processor.Process(this);
    }

    public override string ToString()
    {
      return $"Build generic type instances for '{Type.Name}' type.";
    }


    // Constructors

    public BuildGenericTypeInstancesAction(TypeDef type)
      : base(type)
    {
    }
  }
}