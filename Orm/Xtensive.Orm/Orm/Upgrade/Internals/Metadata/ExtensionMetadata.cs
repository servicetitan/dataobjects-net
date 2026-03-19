// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.02.16

using System;

namespace Xtensive.Orm.Upgrade;

internal sealed class ExtensionMetadata
{
  public string Name { get; }
  public string Value { get; }
  public byte[] Data { get; }

  public override string ToString() => Name;

  // Constructors

  public ExtensionMetadata(string name, string value, byte[] data)
  {
    ArgumentNullException.ThrowIfNull(name);
    Name = name;
    Value = value;
    Data = data;
  }
}
