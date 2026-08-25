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

			builder.Services
				.AddSingleton(sp => {
					var context = WebSocketRemoteConnectionContext.Create(sp, options, configureTransport);
					return ActivatorUtilities.CreateInstance<TConnection>(sp, context);
				})
				.AddSingleton<IRemoteConnection>(sp => sp.GetRequiredService<TConnection>());

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
				.AddSingleton<IRemoteConnectionFactory<TConnection>>(sp =>
					new WebSocketRemoteConnectionFactory<TConnection>(sp, options, configureTransport));

			return builder;

		}

	}

}
