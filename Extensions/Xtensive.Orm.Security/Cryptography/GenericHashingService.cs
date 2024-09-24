// Copyright (C) 2011-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2011.06.10

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Xtensive.Orm.Security.Cryptography
{
  /// <summary>
  /// Generic <see cref="IHashingService"/> implementation.
  /// </summary>
  public abstract class GenericHashingService : IHashingService
  {
    /// <summary>
    /// The size of salt.
    /// </summary>
    public const int SaltSize = 8;

    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>Hash algorithm to use.</returns>
    protected abstract HashAlgorithm GetHashAlgorithm();

    /// <summary>
    /// Gets hash size in bytes for the hash algorithm.
    /// </summary>
    protected abstract int HashSizeInBytes { get; }

    /// <summary>
    /// Fills provided Span with salt.
    /// </summary>
    protected void FillSalt(Span<byte> salt) => RandomNumberGenerator.Fill(salt);

    #region IHashingService Members

    /// <inheritdoc/>
    public string ComputeHash(string password)
    {
      Span<byte> hashWithSalt = stackalloc byte[HashSizeInBytes + SaltSize];
      var salt = hashWithSalt.Slice(HashSizeInBytes);

      FillSalt(salt);
      ComputeHashInternal(password, salt, hashWithSalt.Slice(0, HashSizeInBytes));
      return Convert.ToBase64String(hashWithSalt);
    }

    /// <inheritdoc/>
    public bool VerifyHash(string password, string hash)
    {
      if (hash.Length != Base64.GetMaxEncodedToUtf8Length(HashSizeInBytes + SaltSize)) {
        return false;
      }

      Span<byte> hashWithSalt = stackalloc byte[HashSizeInBytes + SaltSize];

      if (!Convert.TryFromBase64String(hash, hashWithSalt, out var written) || written != hashWithSalt.Length) {
        return false;
      }

      var salt = hashWithSalt.Slice(HashSizeInBytes);
      Span<byte> currentHash = stackalloc byte[HashSizeInBytes];

      ComputeHashInternal(password, salt, currentHash);
      return currentHash.SequenceEqual(hashWithSalt.Slice(0, HashSizeInBytes));
    }

    #endregion

    private void ComputeHashInternal(string password, ReadOnlySpan<byte> salt, Span<byte> hash)
    {
      var encoding = Encoding.UTF8;
      var buffer = ArrayPool<byte>.Shared.Rent(salt.Length + encoding.GetMaxByteCount(password.Length));
      try {
        salt.CopyTo(buffer);
        var written = encoding.GetBytes(password.AsSpan(), buffer.AsSpan(salt.Length));

        using var hasher = GetHashAlgorithm();
        _ = hasher.TryComputeHash(new ReadOnlySpan<byte>(buffer, 0, salt.Length + written), hash, out _);

      }
      finally {
        ArrayPool<byte>.Shared.Return(buffer);
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericHashingService"/> class.
    /// </summary>
    protected GenericHashingService()
    {
    }
  }
}
