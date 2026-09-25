// Copyright (C) 2008-2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2008.02.08

using System;
using System.Globalization;

namespace Xtensive.Conversion
{
  internal class StringRoughAdvancedConverter(IAdvancedConverterProvider provider) :
    RoughAdvancedConverterBase(provider),
    IAdvancedConverter<string, bool>,
    IAdvancedConverter<string, byte>,
    IAdvancedConverter<string, sbyte>,
    IAdvancedConverter<string, short>,
    IAdvancedConverter<string, ushort>,
    IAdvancedConverter<string, int>,
    IAdvancedConverter<string, uint>,
    IAdvancedConverter<string, long>,
    IAdvancedConverter<string, ulong>,
    IAdvancedConverter<string, float>,
    IAdvancedConverter<string, double>,
    IAdvancedConverter<string, decimal>,
    IAdvancedConverter<string, DateTimeOffset>,
    IAdvancedConverter<string, DateTime>,
    IAdvancedConverter<string, DateOnly>,
    IAdvancedConverter<string, TimeOnly>,
    IAdvancedConverter<string, TimeSpan>,
    IAdvancedConverter<string, Guid>,
    IAdvancedConverter<string, char>
  {
    private const string HexPrefix = "0x";
    private static readonly string[] DateTimeFormatStrings = ["yyyy/MM/dd hh:mm:ss.fffffff tt K "];

    bool IAdvancedConverter<string, bool>.Convert(string value) => bool.Parse(value);

    byte IAdvancedConverter<string, byte>.Convert(string value)
    {
      try {
        return byte.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return byte.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    sbyte IAdvancedConverter<string, sbyte>.Convert(string value)
    {
      try {
        return sbyte.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return sbyte.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    short IAdvancedConverter<string, short>.Convert(string value)
    {
      try {
        return short.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return short.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    ushort IAdvancedConverter<string, ushort>.Convert(string value)
    {
      try {
        return ushort.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return ushort.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    int IAdvancedConverter<string, int>.Convert(string value)
    {
      try {
        return int.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return int.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    uint IAdvancedConverter<string, uint>.Convert(string value)
    {
      try {
        return uint.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return uint.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    long IAdvancedConverter<string, long>.Convert(string value)
    {
      try {
        return long.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return long.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    ulong IAdvancedConverter<string, ulong>.Convert(string value)
    {
      try {
        return ulong.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
      }
      catch (FormatException) {
        if (value.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) {
          return ulong.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        throw;
      }
    }

    float IAdvancedConverter<string, float>.Convert(string value) =>
      float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);

    double IAdvancedConverter<string, double>.Convert(string value) =>
      double.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);

    decimal IAdvancedConverter<string, decimal>.Convert(string value) =>
      decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);

    DateTimeOffset IAdvancedConverter<string, DateTimeOffset>.Convert(string value)
    {
      string[] strings = { "yyyy/MM/dd hh:mm:ss.fffffff tt zzz", "yyyy/MM/dd hh:mm:ss.fffffff ttzzz" };
      try {
        return DateTimeOffset.ParseExact(value, strings, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
      }
      catch (FormatException) {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
      }
    }

    DateTime IAdvancedConverter<string, DateTime>.Convert(string value)
    {
      try {
        return DateTime.ParseExact(value, DateTimeFormatStrings, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
      }
      catch (FormatException) {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
      }
    }

    DateOnly IAdvancedConverter<string, DateOnly>.Convert(string value)
    {
      string[] strings = { "yyyy/MM/dd" };
      try {
        return DateOnly.ParseExact(value, strings, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
      }
      catch (FormatException) {
        return DateOnly.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
      }
    }

    TimeOnly IAdvancedConverter<string, TimeOnly>.Convert(string value)
    {
      string[] strings = { "hh:mm:ss.fffffff tt" };
      try {
        return TimeOnly.ParseExact(value, strings, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
      }
      catch (FormatException) {
        return TimeOnly.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
      }
    }

    TimeSpan IAdvancedConverter<string, TimeSpan>.Convert(string value) => TimeSpan.Parse(value);

    Guid IAdvancedConverter<string, Guid>.Convert(string value) => new Guid(value);

    char IAdvancedConverter<string, char>.Convert(string value) => char.Parse(value);
  }
}
