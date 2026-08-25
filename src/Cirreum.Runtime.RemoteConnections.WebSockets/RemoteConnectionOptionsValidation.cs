namespace Cirreum.Runtime;

/// <summary>
/// Validates connection options as they are registered.
/// </summary>
/// <remarks>
/// The transport validates the same options again as it builds a connection. Checking here as
/// well is what makes a misconfiguration surface while the application is composing, rather
/// than when something first resolves the connection.
/// </remarks>
internal static class RemoteConnectionOptionsValidation {

	public static void Validate(RemoteConnectionOptions options, Type connectionType) {

		if (!options.EndpointUri.OriginalString.HasValue()) {
			throw new InvalidOperationException(
				$"'{connectionType.Name}' requires an {nameof(RemoteConnectionOptions.EndpointUri)}.");
		}

		if (!options.EndpointUri.IsAbsoluteUri) {
			throw new InvalidOperationException(
				$"'{connectionType.Name}' requires an absolute {nameof(RemoteConnectionOptions.EndpointUri)}. " +
				$"Unsupported: {options.EndpointUri}");
		}

		if (options.ReconnectMaxDelay <= TimeSpan.Zero) {
			throw new InvalidOperationException(
				$"'{connectionType.Name}' requires a {nameof(RemoteConnectionOptions.ReconnectMaxDelay)} greater than zero.");
		}

	}

}
