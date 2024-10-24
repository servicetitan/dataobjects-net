// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.06.12

using Xtensive.Orm.Building.Definitions;

namespace Xtensive.Orm.Building.FixupActions
{
  internal sealed class AddPrimaryIndexAction : TypeAction
  {
    public override void Run(FixupActionProcessor processor)
    {
      processor.Process(this);
    }

    public override string ToString()
    {
      return $"Add primary index to '{Type.Name}'";
    }

    // Constructors

    public AddPrimaryIndexAction(TypeDef typeDef)
      : base(typeDef)
    {
    }
  }
}