namespace Cirreum.Runtime;

using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Creates per-session <typeparamref name="TConnection"/> instances from the registered options.
/// </summary>
internal sealed class WebSocketRemoteConnectionFactory<TConnection>(
	IServiceProvider services,
	RemoteConnectionOptions options,
	Action<ClientWebSocketOptions>? configureTransport)
	: IRemoteConnectionFactory<TConnection>
	where TConnection : WebSocketRemoteConnection {

	public TConnection Create(Action<RemoteConnectionOptions>? configure = null) {

		// Each instance configures a copy: an adjustment made for one session must not reach
		// the registration every later session builds from.
		var instanceOptions = Copy(options);
		configure?.Invoke(instanceOptions);

		var context = WebSocketRemoteConnectionContext.Create(services, instanceOptions, configureTransport);
		return ActivatorUtilities.CreateInstance<TConnection>(services, context);

	}

	private static RemoteConnectionOptions Copy(RemoteConnectionOptions source) {
		return new RemoteConnectionOptions(source.ApplicationName) {
			EndpointUri = source.EndpointUri,
			AuthorizationHeader = source.AuthorizationHeader,
			AccessTokenProvider = source.AccessTokenProvider,
			Reconnect = source.Reconnect,
			ReconnectMaxDelay = source.ReconnectMaxDelay,
		};
	}

}
