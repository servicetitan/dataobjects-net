// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;


namespace Xtensive.Modelling.Actions
{
  /// <summary>
  /// Path node reference.
  /// </summary>
  [Serializable]
  public readonly struct PathNodeReference :
    IEquatable<PathNodeReference>
  {
    private readonly string path;

    /// <summary>
    /// Gets the path to the node.
    /// </summary>
    public string Path {
      get { return path; }
    }

    /// <summary>
    /// Gets the <see cref="PathNodeReference"/> to the specified source,
    /// if the source is <see cref="IPathNode"/>;
    /// otherwise, returns <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns><see cref="PathNodeReference"/> to the specified source,
    /// if the source is <see cref="IPathNode"/>;
    /// otherwise, returns <paramref name="source"/>.</returns>
    public static object Get(object source)
    {
      var pathNode = source as IPathNode;
      if (pathNode==null)
        return source;
      return new PathNodeReference(pathNode.Path);
    }

    /// <summary>
    /// Resolves the specified object (possibly <see cref="PathNodeReference"/>).
    /// Reverts the effect of <see cref="Get"/> method.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="source">The object to resolve.</param>
    /// <returns>Either original object, or
    /// resolved <see cref="PathNodeReference"/> (<see cref="IPathNode"/>)</returns>
    public static object Resolve(IModel model, object source) =>
      source is PathNodeReference pnr ? model.Resolve(pnr.Path, true) : source;

    #region Equality members

    /// <inheritdoc/>
    public bool Equals(PathNodeReference other) => path == other.path;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is PathNodeReference other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => path != null ? path.GetHashCode() : 0;

    public static bool operator ==(PathNodeReference left, PathNodeReference right) => left.Equals(right);

    public static bool operator !=(PathNodeReference left, PathNodeReference right) => !left.Equals(right);

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
      return path;
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="path">The <see cref="Path"/> value.</param>
    public PathNodeReference(string path)
    {
      this.path = path;
    }
  }
}