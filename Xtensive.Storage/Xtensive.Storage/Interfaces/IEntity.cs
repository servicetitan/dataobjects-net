// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2007.05.25

using System.ComponentModel;
using Xtensive.Core;
using Xtensive.Storage.Model;
using Xtensive.Storage.Rse;

namespace Xtensive.Storage
{
  /// <summary>
  /// Should be implemented by any persistent entity.
  /// </summary>
  [SystemType]
  public interface IEntity: 
    IIdentified<Key>, 
    IHasVersion<VersionInfo>,
    INotifyPropertyChanged
  {
    /// <summary>
    /// Gets the <see cref="Key"/> of the <see cref="Entity"/>.
    /// </summary>
    Key Key { get; }

    /// <summary>
    /// Gets <see cref="VersionInfo"/> object describing 
    /// current version of the <see cref="Entity"/>.
    /// </summary>
    VersionInfo VersionInfo { get; }

    /// <summary>
    /// Gets <see cref="TypeInfo"/> object describing <see cref="Entity"/> structure.
    /// </summary>
    TypeInfo Type { get; }

    /// <summary>
    /// Gets the type id.
    /// </summary>
    [Field]
    int TypeId { get; }

    /// <summary>
    /// Gets or sets the value of the field with specified name.
    /// </summary>
    /// <value>Field value.</value>
    object this[string fieldName] { get; set; }

    /// <summary>
    /// Gets persistence state of the entity.
    /// </summary>
    PersistenceState PersistenceState { get; }

    /// <summary>
    /// Gets a value indicating whether this entity is removed.
    /// </summary>
    /// <seealso cref="Remove"/>
    bool IsRemoved { get; }

    /// <summary>
    /// Removes the instance.
    /// </summary>
    void Remove();

    /// <summary>
    /// Registers the instance in the removal queue.
    /// </summary>
    void RemoveLater();

    /// <summary>
    /// Locks this instance in the storage.
    /// </summary>
    /// <param name="lockMode">The lock mode.</param>
    /// <param name="lockBehavior">The lock behavior.</param>
    void Lock(LockMode lockMode, LockBehavior lockBehavior);

  }
}