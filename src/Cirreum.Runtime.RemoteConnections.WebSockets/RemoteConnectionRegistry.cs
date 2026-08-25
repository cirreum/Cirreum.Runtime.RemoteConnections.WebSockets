namespace Cirreum.Runtime;

using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

/// <summary>
/// Tracks which connection types a service collection has already registered.
/// </summary>
/// <remarks>
/// Keyed by service collection rather than held process-wide: a second container in the same
/// process — a test host, a second builder — composes its own registrations, and a shared
/// registry would make it skip them because the first container had claimed the type.
/// </remarks>
internal static class RemoteConnectionRegistry {

	private sealed record Registration(RemoteConnectionOptions Options, bool IsFactory);

	private static readonly ConditionalWeakTable<IServiceCollection, Dictionary<Type, Registration>> RegistrationsByCollection = [];

	/// <summary>
	/// Records a registration for <paramref name="connectionType"/>, returning whether the
	/// caller should proceed. Returns <see langword="false"/> when an equal registration
	/// already exists; throws when a conflicting one does.
	/// </summary>
	public static bool TryRegister(
		IServiceCollection services,
		Type connectionType,
		RemoteConnectionOptions options,
		bool isFactory) {

		var registrations = RegistrationsByCollection.GetValue(services, static _ => []);

		if (registrations.TryGetValue(connectionType, out var existing)) {

			if (existing.IsFactory != isFactory) {
				throw new InvalidOperationException(
					$"'{connectionType.Name}' is already registered as " +
					$"{(existing.IsFactory ? "a connection factory" : "a connection")} and cannot also be registered as " +
					$"{(isFactory ? "a connection factory" : "a connection")}. " +
					$"Subclass the connection to register a second endpoint.");
			}

			if (!existing.Options.Equals(options)) {
				throw new InvalidOperationException(
					$"'{connectionType.Name}' is already registered with different options. " +
					$"Subclass the connection to register a second endpoint.");
			}

			return false;
		}

		registrations.Add(connectionType, new Registration(options, isFactory));
		return true;

	}

}
