// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.04.15

using System.Diagnostics;
using System.Runtime.Serialization;
using Xtensive.Core;
using Xtensive.Orm.Rse.Providers;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Rse
{
  /// <summary>
  /// A parameter for accessing current tuple of left (outer) <see cref="Provider"/>
  /// within right (inner) <see cref="Provider"/>.
  /// </summary>
  [Serializable]
  [DebuggerDisplay("{Name}")]
  public readonly record struct ApplyParameter(string Name)
  {
    [NonSerialized]
    private readonly Parameter<Tuple> parameter = new(Name);

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <value>The value.</value>
    public Tuple Value {
      [DebuggerStepThrough]
      get { return parameter.Value; }
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Name", Name);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      return Name;
    }


    // Constructors


    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    public ApplyParameter()
      : this(string.Empty)
    {
    }

    public ApplyParameter(SerializationInfo info, StreamingContext context)
      : this(info.GetString("Name"))
    {
    }
  }
}
