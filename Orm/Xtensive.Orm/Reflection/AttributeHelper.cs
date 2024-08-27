// Copyright (C) 2008-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2008.06.13

using System;
using System.Collections.Concurrent;
using System.Reflection;
using PerAttributeKey = System.ValueTuple<System.ModuleHandle, int, Xtensive.Reflection.AttributeSearchOptions>;

namespace Xtensive.Reflection
{
  /// <summary>
  /// <see cref="Attribute"/> related helper \ extension methods.
  /// </summary>
  public static class AttributeHelper
  {
    private static class AttributeDictionary<TAttribute> where TAttribute : Attribute
    {
      private static readonly Type attributeType = typeof(TAttribute);
      public static readonly ConcurrentDictionary<PerAttributeKey, TAttribute[]> Dictionary = new();

      public static readonly Func<PerAttributeKey, MemberInfo, TAttribute[]> AttributesExtractor = ExtractAttributesByKey;

      private static TAttribute[] ExtractAttributesByKey(PerAttributeKey key, MemberInfo member)
      {
        var (_, _, options) = key;

        var attributes = (TAttribute[]) member.GetCustomAttributes(attributeType, false);

        if (options != AttributeSearchOptions.InheritNone) {
          if (attributes.Length == 0) {
            if ((options & AttributeSearchOptions.InheritFromPropertyOrEvent) != 0
                && member is MethodInfo m
                && ((MemberInfo) m.GetProperty() ?? m.GetEvent()) is { } poe) {
              attributes = (TAttribute[]) poe.GetCustomAttributes(attributeType, false);
            }
            if ((options & AttributeSearchOptions.InheritFromBase) != 0
                && (options & AttributeSearchOptions.InheritFromAllBase) == 0) {
              AddAttributesFromBase(ref attributes, member, options);
            }
          }

          if ((options & AttributeSearchOptions.InheritFromAllBase) != 0
              && member.DeclaringType != WellKnownTypes.Object) {
            AddAttributesFromBase(ref attributes, member, options);
          }
        }

        return attributes ?? Array.Empty<TAttribute>();
      }

      private static void AddAttributesFromBase(ref TAttribute[] attributes, MemberInfo member, AttributeSearchOptions options)
      {
        if (member.GetBaseMember() is { } bm) {
          var attrsToAdd = bm.GetAttributes<TAttribute>(options);
          if (attrsToAdd.Length > 0) {
            if (attributes?.Length > 0) {
              var newArr = new TAttribute[attributes.Length + attrsToAdd.Length];
              attributes.CopyTo(newArr, 0);
              attrsToAdd.CopyTo(newArr, attributes.Length);
              attributes = newArr;
            }
            else {
              attributes = attrsToAdd;
            }
          }
        }
      }
    }

    /// <summary>
    /// A shortcut to <see cref="MemberInfo.GetCustomAttributes(Type,bool)"/> method.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attributes to get.</typeparam>
    /// <param name="member">Member to get attributes of.</param>
    /// <param name="options">Attribute search options.</param>
    /// <returns>An array of attributes of specified type.</returns>
    ///
    public static TAttribute[] GetAttributes<TAttribute>(this MemberInfo member, AttributeSearchOptions options = AttributeSearchOptions.InheritNone)
        where TAttribute : Attribute =>
      AttributeDictionary<TAttribute>.Dictionary.GetOrAdd(new PerAttributeKey(member.Module.ModuleHandle, member.MetadataToken, options), AttributeDictionary<TAttribute>.AttributesExtractor, member);

    /// <summary>
    /// A version of <see cref="GetAttributes{TAttribute}(MemberInfo, AttributeSearchOptions)"/>
    /// returning just one attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to get.</typeparam>
    /// <param name="member">Member to get attribute of.</param>
    /// <param name="options">Attribute search options.</param>
    /// <returns>An attribute of specified type;
    /// <see langword="null"/>, if there is no such attribute;
    /// throws <see cref="InvalidOperationException"/>, if there is more then one attribute of specified type found.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if there is more then one attribute of specified type found.</exception>
    public static TAttribute GetAttribute<TAttribute>(this MemberInfo member, AttributeSearchOptions options = AttributeSearchOptions.InheritNone)
      where TAttribute : Attribute
    {
      var attributes = member.GetAttributes<TAttribute>(options);
      return attributes.Length switch {
        0 => null,
        1 => attributes[0],
        _ => throw new InvalidOperationException(string.Format(Strings.ExMultipleAttributesOfTypeXAreNotAllowedHere,
          member.GetShortName(true),
          typeof(TAttribute).GetShortName()))
      };
    }
  }
}
