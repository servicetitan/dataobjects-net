// Copyright (C) 2013 Xtensive LLC
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.07.18

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Xtensive.Core
{
  /// <summary>
  /// Convenient wrapper for <see cref="XmlSerializer"/>.
  /// </summary>
  public sealed class SimpleXmlSerializer<T>
    where T : class
  {
    private static readonly XmlWriterSettings WriterSettings = new() {
      Encoding = new UTF8Encoding(false),
      Indent = false,
      NewLineChars = "\n"
    };

    private readonly XmlSerializer serializer = new XmlSerializer(typeof (T));

    /// <summary>
    /// Deserializes value of <typeparamref name="T"/> from string.
    /// </summary>
    /// <param name="value">Serialized instance.</param>
    /// <returns>Deserialized instance.</returns>
    public T Deserialize(string value)
    {
      ArgumentNullException.ThrowIfNull(value, "serialized");

      using (var reader = new StringReader(value))
        return (T) serializer.Deserialize(reader);
    }

    /// <summary>
    /// Serializes value of <typeparamref name="T"/> to string.
    /// </summary>
    /// <param name="value">Instance to serialize.</param>
    /// <returns>Serialized instance.</returns>
    public string Serialize(T value)
    {
      ArgumentNullException.ThrowIfNull(value);

      using StringWriter stringWriter = new();
      using (var xmlWriter = XmlWriter.Create(stringWriter, WriterSettings)) {
        serializer.Serialize(xmlWriter, value);
      }
      return stringWriter.ToString();
    }
  }
}
