// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.05.22

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Serialization;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Model.Stored.Internals;

namespace Xtensive.Orm.Model.Stored
{
  /// <summary>
  /// An xml serializable representation of <see cref="DomainModel"/>.
  /// </summary>
  [XmlRoot("DomainModel", Namespace = "")]
  public sealed class StoredDomainModel
  {
    private static readonly SimpleXmlSerializer<StoredDomainModel> Serializer = new();

    /// <summary>
    /// <see cref="DomainModel.Types"/>.
    /// </summary>
    [XmlArray("Types"), XmlArrayItem("Type")]
    public StoredTypeInfo[] Types;

    /// <summary>
    /// <see cref="DomainModel.Associations"/>
    /// </summary>
    [XmlIgnore]
    public StoredAssociationInfo[] Associations;

    /// <summary>
    /// <see cref="DomainModel.Hierarchies"/>
    /// </summary>
    [XmlIgnore]
    public StoredHierarchyInfo[] Hierarchies;

    /// <summary>
    /// Deserializes <see cref="StoredDomainModel"/> from string.
    /// </summary>
    /// <param name="serialized">Serialized instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static StoredDomainModel Deserialize(string serialized, byte[] data)
    {
      if (data != null) {
        switch (data[0]) {
          case 0:
            return Serializer.Deserialize(Encoding.UTF8.GetString(data, 1, data.Length - 1));
          case 1:
            using BrotliStream brotliStream = new(new MemoryStream(data, 1, data.Length - 1), CompressionMode.Decompress);
            return Serializer.DeserializeFromStream(brotliStream);
          default:
            throw new NotSupportedException("Invalid data format");
        }
      }
      return Serializer.Deserialize(serialized);
    }

    /// <summary>
    /// Serializes this instance to string.
    /// </summary>
    /// <returns>Serialized instance.</returns>
    public (string Xml, byte[] Compressed) Serialize()
    {
      MemoryStream ms = new(1000);
      ms.WriteByte(1);      // 1 - means Brotli method
      using (BrotliStream brotliStream = new(ms, CompressionLevel.Optimal)) {
        Serializer.SerializeIntoStream(this, brotliStream);
      }
      return (null, ms.ToArray());
    }

    /// <summary>
    /// Updates references between nodes.
    /// </summary>
    public void UpdateReferences()
    {
      new ReferenceUpdater().UpdateReferences(this);
    }

    /// <summary>
    /// Updates type mappings to database or scheme according to <paramref name="nodeConfiguration"/>
    /// </summary>
    /// <param name="nodeConfiguration">The node configuration</param>
    public void UpdateMappings(NodeConfiguration nodeConfiguration)
    {
      if (nodeConfiguration==null)
        return;
      new TypeMappingUpdater().UpdateMappings(this, nodeConfiguration);
    }
  }
}
