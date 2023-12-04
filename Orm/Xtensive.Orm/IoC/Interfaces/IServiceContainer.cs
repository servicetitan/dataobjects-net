// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2010.01.30

using System;
using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Reflection;

namespace Xtensive.IoC
{
  /// <summary>
  /// Inversion of control container contract.
  /// </summary>
  public interface IServiceContainer : IServiceProvider, 
    IHasServices,
    IDisposable
  {
    /// <summary>
    /// Gets the parent service container.
    /// Parent service container usually resolves services that 
    /// can't be resolved by the current container.
    /// </summary>
    IServiceContainer Parent { get; }

    /// <summary>
    /// Gets all the instances of type <typeparamref name="TService"/>
    /// from the container.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>
    /// A sequence of all the requested instances.
    /// </returns>
    IEnumerable<TService> GetAll<TService>();

    /// <summary>
    /// Gets all the instances of type <paramref name="serviceType"/>
    /// from the container.
    /// </summary>
    /// <param name="serviceType">Type of the service.</param>
    /// <returns>
    /// A sequence of all the requested instances.
    /// </returns>
    IEnumerable<object> GetAll(Type serviceType);

    /// <summary>
    /// Gets the instance of <typeparamref name="TService"/> type
    /// from the container.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>Requested instance.</returns>
    TService Get<TService>();

    /// <summary>
    /// Gets the instance of <typeparamref name="TService"/> type
    /// identified by the specified <paramref name="name"/>
    /// from the container.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="name">The identifier of the service to get.</param>
    /// <returns>Requested instance.</returns>
    TService Get<TService>(string name);

    /// <summary>
    /// Gets the instance of <paramref name="serviceType"/>
    /// from the container.
    /// </summary>
    /// <param name="serviceType">Type of the service.</param>
    /// <returns>Requested instance.</returns>
    object Get(Type serviceType);

    /// <summary>
    /// Gets the instance of <paramref name="serviceType"/>
    /// identified by the specified <paramref name="name"/>
    /// from the container.
    /// </summary>
    /// <param name="serviceType">Type of the service.</param>
    /// <param name="name">The identifier of the service to get.</param>
    /// <returns>Requested instance.</returns>
    object Get(Type serviceType, string name);

    /// <summary>
    /// Demands the specified service 
    /// using <see cref="IServiceContainer.Get{TService}()"/> method
    /// and ensures the result is not <see langword="null" />.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="container">The container to demand the service on.</param>
    /// <returns></returns>
    /// <exception cref="ActivationException">There was an error on activation of some instance(s),
    /// or service is not available.</exception>
    public TService Demand<TService>() =>
      EnsureNotNull(Get<TService>(), null);

    /// <summary>
    /// Demands the specified service
    /// using <see cref="IServiceContainer.Get{TService}(string)"/> method
    /// and ensures the result is not <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="name">The service name.</param>
    /// <returns></returns>
    /// <exception cref="ActivationException">There was an error on activation of some instance(s),
    /// or service is not available.</exception>
    public TService Demand<TService>(string name) =>
      EnsureNotNull(Get<TService>(name), name);

    /// <summary>
    /// Demands the specified service
    /// using <see cref="IServiceContainer.Get(System.Type)"/> method
    /// and ensures the result is not <see langword="null"/>.
    /// </summary>
    /// <param name="serviceType">Type of the service.</param>
    /// <returns></returns>
    /// <exception cref="ActivationException">There was an error on activation of some instance(s),
    /// or service is not available.</exception>
    public object Demand(Type serviceType) =>
      EnsureNotNull(Get(serviceType), serviceType, null);

    /// <summary>
    /// Demands the specified service
    /// using <see cref="IServiceContainer.Get(System.Type,string)"/> method
    /// and ensures the result is not <see langword="null"/>.
    /// </summary>
    /// <param name="container">The container to demand the service on.</param>
    /// <param name="serviceType">Type of the service.</param>
    /// <param name="name">The service name.</param>
    /// <returns></returns>
    /// <exception cref="ActivationException">There was an error on activation of some instance(s),
    /// or service is not available.</exception>
    public object Demand(Type serviceType, string name) =>
      EnsureNotNull(Get(serviceType, name), serviceType, name);

    #region Private / internal methods

    private static TService EnsureNotNull<TService>(TService service, string name)
    {
      if (service == null) {
        EnsureNotNull(service, typeof(TService), name);
      }
      return service;
    }

    /// <exception cref="ActivationException">Service is not available.</exception>
    private static object EnsureNotNull(object service, Type serviceType, string name)
    {
      if (service != null)
        return service;
      if (name == null)
        throw new ActivationException(string.Format(Strings.ExServiceOfTypeXIsNotAvailable, serviceType.GetShortName()));
      throw new ActivationException(string.Format(Strings.ExServiceWithNameXOfTypeYIsNotAvailable, name, serviceType.GetShortName()));
    }

    #endregion

  }
}
