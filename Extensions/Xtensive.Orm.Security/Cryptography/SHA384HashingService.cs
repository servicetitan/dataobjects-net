// Copyright (C) 2011-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2011.05.22

using System.Security.Cryptography;
using Xtensive.IoC;

namespace Xtensive.Orm.Security.Cryptography
{
  /// <summary>
  /// Implementation of <see cref="IHashingService"/> with SHA384 algorithm.
  /// </summary>
  [Service(typeof (IHashingService), Singleton = true, Name = "sha384")]
  public class SHA384HashingService : GenericHashingService
  {
    /// <inheritdoc/>
#pragma warning disable SYSLIB0021 // Type or member is obsolete
    // direct creation is more efficient than SHA384.Create()
    protected override HashAlgorithm GetHashAlgorithm() => new SHA384Managed();
#pragma warning restore SYSLIB0021 // Type or member is obsolete

    /// <inheritdoc />
    protected override int HashSizeInBytes => SHA384.HashSizeInBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SHA384HashingService"/> class.
    /// </summary>
    public SHA384HashingService()
    {
    }
  }
}
