// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.11.01

using System;
using System.Collections.Concurrent;
using System.Reflection;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals
{
  internal static class Activator
  {
    private static readonly Assembly OrmAssembly = typeof (Activator).Assembly;

    private const string OrmFactoryMethodName = "CreateObject";
    private const string OtherFactoryMethodName = "~Xtensive.Orm.CreateObject";

    private static class Traits<TArg1, TArg2, TResult> where TArg1 : class where TArg2 : class
    {
      private static readonly Type DelegateType = typeof(Func<TArg1, TArg2, TResult>);
      private static readonly Type[] TypeParams = [typeof(TArg1), typeof(TArg2)];

      public static readonly Func<Type, Func<TArg1, TArg2, TResult>> Activator = type => {
        var methodName = type.Assembly == OrmAssembly ? OrmFactoryMethodName : OtherFactoryMethodName;
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic, null, TypeParams, null)
                     ?? throw new InvalidOperationException(string.Format(
                       Strings.ExUnableToFindFactoryMethodForTypeXMakeSureAssemblyYProcessedByWeaver,
                       type.FullName, type.Assembly));
        return (Func<TArg1, TArg2, TResult>) Delegate.CreateDelegate(DelegateType, method);
      };
    }

    private static readonly ConcurrentDictionary<Type, Func<Session, EntityState, Entity>> EntityActivators = new();
    private static readonly ConcurrentDictionary<Type, Func<Session, Tuple, Structure>> DetachedStructureActivators = new();
    private static readonly ConcurrentDictionary<Type, Func<Persistent, FieldInfo, Structure>> StructureActivators = new();
    private static readonly ConcurrentDictionary<Type, Func<Entity, FieldInfo, EntitySetBase>> EntitySetActivators = new();

    public static Entity CreateEntity(Session session, Type type, EntityState state) =>
      EntityActivators.GetOrAdd(type, Traits<Session, EntityState, Entity>.Activator).Invoke(session, state);

    public static Structure CreateStructure(Type type, Persistent owner, FieldInfo field)
    {
      var result = StructureActivators.GetOrAdd(type, Traits<Persistent, FieldInfo, Structure>.Activator).Invoke(owner, field);
      result.SystemInitialize(true);
      return result;
    }

    public static Structure CreateStructure(Session session, Type type, Tuple tuple) =>
      DetachedStructureActivators.GetOrAdd(type, Traits<Session, Tuple, Structure>.Activator).Invoke(session, tuple);

    public static EntitySetBase CreateEntitySet(Entity owner, FieldInfo field) =>
      EntitySetActivators.GetOrAdd(field.ValueType, Traits<Entity, FieldInfo, EntitySetBase>.Activator).Invoke(owner, field);
  }
}