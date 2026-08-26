namespace Cirreum.Runtime;

using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration of raw WebSocket remote connections on a Cirreum application builder.
/// </summary>
public static class RemoteConnectionHostingExtensions {

	extension(IDomainApplicationBuilder builder) {

		/// <summary>
		/// Registers <typeparamref name="TConnection"/> as a single connection living for the
		/// lifetime of the application.
		/// </summary>
		/// <typeparam name="TConnection">The connection type to register.</typeparam>
		/// <param name="configure">Configures the connection's options.</param>
		/// <param name="configureTransport">
		/// An optional delegate applied to the underlying <see cref="ClientWebSocketOptions"/>
		/// after the framework has configured it.
		/// </param>
		/// <remarks>
		/// The connection resolves as <typeparamref name="TConnection"/> and as
		/// <see cref="IRemoteConnection"/>, and is disposed with the host. A type may be
		/// registered once: subclass the connection to reach a second endpoint.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// <typeparamref name="TConnection"/> is already registered with different options, or
		/// as a connection factory.
		/// </exception>
		public IDomainApplicationBuilder AddRemoteConnection<TConnection>(
			Action<RemoteConnectionOptions> configure,
			Action<ClientWebSocketOptions>? configureTransport = null)
			where TConnection : WebSocketRemoteConnection {

			ArgumentNullException.ThrowIfNull(configure);

			var options = new RemoteConnectionOptions();
			configure(options);

			return builder.AddRemoteConnection<TConnection>(options, configureTransport);

		}

		/// <summary>
		/// Registers <typeparamref name="TConnection"/> as a single connection living for the
		/// lifetime of the application.
		/// </summary>
		/// <typeparam name="TConnection">The connection type to register.</typeparam>
		/// <param name="options">The connection's options.</param>
		/// <param name="configureTransport">
		/// An optional delegate applied to the underlying <see cref="ClientWebSocketOptions"/>
		/// after the framework has configured it.
		/// </param>
		/// <remarks>
		/// The connection resolves as <typeparamref name="TConnection"/> and as
		/// <see cref="IRemoteConnection"/>, and is disposed with the host. A type may be
		/// registered once: subclass the connection to reach a second endpoint.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// <typeparamref name="TConnection"/> is already registered with different options, or
		/// as a connection factory.
		/// </exception>
		public IDomainApplicationBuilder AddRemoteConnection<TConnection>(
			RemoteConnectionOptions options,
			Action<ClientWebSocketOptions>? configureTransport = null)
			where TConnection : WebSocketRemoteConnection {

			ArgumentNullException.ThrowIfNull(options);
			RemoteConnectionOptionsValidation.Validate(options, typeof(TConnection));

			if (!RemoteConnectionRegistry.TryRegister(builder.Services, typeof(TConnection), options, isFactory: false)) {
				return builder;
			}

			var lifetime = LifetimeFor(OperatingSystem.IsBrowser());

			builder.Services
				.Add(new ServiceDescriptor(typeof(TConnection), sp => {
					var context = WebSocketRemoteConnectionContext.Create<TConnection>(sp, options, configureTransport);
					return ActivatorUtilities.CreateInstance<TConnection>(sp, context);
				}, lifetime));

			builder.Services
				.Add(new ServiceDescriptor(typeof(IRemoteConnection),
					sp => sp.GetRequiredService<TConnection>(), lifetime));

			return builder;

		}

		/// <summary>
		/// Registers a factory that creates <typeparamref name="TConnection"/> instances, each
		/// owned by the caller that created it.
		/// </summary>
		/// <typeparam name="TConnection">The connection type the factory creates.</typeparam>
		/// <param name="configure">Configures the options each instance is created from.</param>
		/// <param name="configureTransport">
		/// An optional delegate applied to the underlying <see cref="ClientWebSocketOptions"/>
		/// after the framework has configured it.
		/// </param>
		/// <remarks>
		/// Registers <see cref="IRemoteConnectionFactory{TConnection}"/> and no connection
		/// instance. The caller connects, uses, and disposes what it creates. A type may be
		/// registered once: subclass the connection to reach a second endpoint.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// <typeparamref name="TConnection"/> is already registered with different options, or
		/// as a single connection.
		/// </exception>
		public IDomainApplicationBuilder AddRemoteConnectionFactory<TConnection>(
			Action<RemoteConnectionOptions> configure,
			Action<ClientWebSocketOptions>? configureTransport = null)
			where TConnection : WebSocketRemoteConnection {

			ArgumentNullException.ThrowIfNull(configure);

			var options = new RemoteConnectionOptions();
			configure(options);

			return builder.AddRemoteConnectionFactory<TConnection>(options, configureTransport);

		}

		/// <summary>
		/// Registers a factory that creates <typeparamref name="TConnection"/> instances, each
		/// owned by the caller that created it.
		/// </summary>
		/// <typeparam name="TConnection">The connection type the factory creates.</typeparam>
		/// <param name="options">The options each instance is created from.</param>
		/// <param name="configureTransport">
		/// An optional delegate applied to the underlying <see cref="ClientWebSocketOptions"/>
		/// after the framework has configured it.
		/// </param>
		/// <remarks>
		/// Registers <see cref="IRemoteConnectionFactory{TConnection}"/> and no connection
		/// instance. The caller connects, uses, and disposes what it creates. A type may be
		/// registered once: subclass the connection to reach a second endpoint.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// <typeparamref name="TConnection"/> is already registered with different options, or
		/// as a single connection.
		/// </exception>
		public IDomainApplicationBuilder AddRemoteConnectionFactory<TConnection>(
			RemoteConnectionOptions options,
			Action<ClientWebSocketOptions>? configureTransport = null)
			where TConnection : WebSocketRemoteConnection {

			ArgumentNullException.ThrowIfNull(options);
			RemoteConnectionOptionsValidation.Validate(options, typeof(TConnection));

			if (!RemoteConnectionRegistry.TryRegister(builder.Services, typeof(TConnection), options, isFactory: true)) {
				return builder;
			}

			builder.Services
				.Add(new ServiceDescriptor(typeof(IRemoteConnectionFactory<TConnection>),
					sp => new WebSocketRemoteConnectionFactory<TConnection>(sp, options, configureTransport),
					LifetimeFor(OperatingSystem.IsBrowser())));

			return builder;

		}

	}

	/// <summary>
	/// The lifetime a connection is registered with, which differs by host.
	/// </summary>
	/// <remarks>
	/// A browser has exactly one scope, created by the host and living as long as the application,
	/// and every service the application composes - the access-token provider among them - is
	/// registered against it. A singleton lives in the root container instead, where none of them
	/// can be reached. Scoped is therefore the application lifetime there.
	/// <para>
	/// On a server the reverse holds: a scope is a request, so a scoped connection would be a new
	/// connection for every one of them.
	/// </para>
	/// </remarks>
	internal static ServiceLifetime LifetimeFor(bool isBrowser) =>
		isBrowser ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;

}
