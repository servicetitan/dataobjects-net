// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.07.11

using System.Diagnostics;
using Xtensive.Storage.Providers;

namespace Xtensive.Storage.Providers
{
  /// <summary>
  /// Storage handler accessor.
  /// Provided by protected members, such as <see cref="HandlerBase.Handlers"/> 
  /// to provide access to other available handlers.
  public sealed class HandlerAccessor
  {
    /// <summary>
    /// Gets the <see cref="Xtensive.Storage.Domain"/> 
    /// this handler accessor is bound to.
    /// </summary>
    [DebuggerHidden]
    public Domain Domain { get; private set; }

    /// <summary>
    /// Gets the handler provider 
    /// creating handlers in the <see cref="Domain"/>.
    /// </summary>
    [DebuggerHidden]
    public HandlerFactory HandlerFactory { get; internal set; }

    /// <summary>
    /// Gets the name builder.
    /// </summary>
    [DebuggerHidden]
    public NameBuilder NameBuilder { get; internal set; }

    /// <summary>
    /// Gets the key manager.
    /// </summary>
    [DebuggerHidden]
    public KeyManager KeyManager { get; internal set; }

    /// <summary>
    /// Gets the <see cref="Domain"/> handler.
    /// </summary>
    [DebuggerHidden]
    public DomainHandler DomainHandler { get; internal set; }

    /// <summary>
    /// Gets the handler of the current <see cref="Session"/>.
    /// </summary>
    [DebuggerHidden]
    public SessionHandler SessionHandler
    {
      get { return Session.Current.Handler; }
    }


    // Constructors

    internal HandlerAccessor(Domain domain)
    {
      Domain = domain;
    }
  }
}