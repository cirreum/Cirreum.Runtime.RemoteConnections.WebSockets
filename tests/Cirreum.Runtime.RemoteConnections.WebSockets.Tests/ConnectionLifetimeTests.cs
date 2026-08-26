namespace Cirreum.Runtime.RemoteConnections.WebSockets.Tests;

using Cirreum.Runtime;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A connection's registered lifetime differs by host, and the reason is a DI mechanic that is
/// easy to reason past: in a browser the application lives in a scope, not in the root.
/// </summary>
public class ConnectionLifetimeTests {

	// ---------------------------------------------------------------------
	// The rule
	// ---------------------------------------------------------------------

	[Fact]
	public void InABrowser_AConnectionIsScoped() {
		RemoteConnectionHostingExtensions.LifetimeFor(isBrowser: true)
			.Should().Be(ServiceLifetime.Scoped);
	}

	[Fact]
	public void OnAServer_AConnectionIsASingleton() {
		// A scope is a request there, so a scoped connection would be a new connection per request.
		RemoteConnectionHostingExtensions.LifetimeFor(isBrowser: false)
			.Should().Be(ServiceLifetime.Singleton);
	}

	// ---------------------------------------------------------------------
	// Why — the mechanic the rule exists for
	// ---------------------------------------------------------------------

	private interface ITokenProviderLike {
		int Id { get; }
	}

	private sealed class TokenProvider : ITokenProviderLike {
		private static int _next;
		public int Id { get; } = Interlocked.Increment(ref _next);
	}

	private sealed class CredentialSource(ITokenProviderLike tokens) {
		public ITokenProviderLike Tokens => tokens;
	}

	/// <summary>Stands in for a connection: it captures the provider it was built from.</summary>
	private sealed class CapturingConnection(IServiceProvider captured) {
		public IServiceProvider Captured => captured;
	}

	private static IServiceCollection Host(ServiceLifetime connectionLifetime) {
		IServiceCollection services = new ServiceCollection();
		services.AddScoped<ITokenProviderLike, TokenProvider>();
		services.AddScoped<CredentialSource>();
		services.Add(new ServiceDescriptor(
			typeof(CapturingConnection), sp => new CapturingConnection(sp), connectionLifetime));
		return services;
	}

	[Fact]
	public void ASingletonConnection_CannotReachAScopedCredentialSource() {

		// A singleton factory receives the root provider, and scope validation refuses a scoped
		// service from it.
		var provider = Host(ServiceLifetime.Singleton)
			.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var appScope = provider.CreateScope();

		var connection = appScope.ServiceProvider.GetRequiredService<CapturingConnection>();

		var act = () => connection.Captured.GetRequiredService<CredentialSource>();

		act.Should().Throw<InvalidOperationException>().WithMessage("*scoped service*");

	}

	[Fact]
	public void WithoutValidation_ASingletonConnection_GetsADifferentGraphThanTheApplication() {

		// The quieter half of the same fault: no exception, a second token provider resolved into
		// the root, and an application that cannot tell.
		var provider = Host(ServiceLifetime.Singleton).BuildServiceProvider();
		using var appScope = provider.CreateScope();

		var fromApp = appScope.ServiceProvider.GetRequiredService<CredentialSource>();
		var connection = appScope.ServiceProvider.GetRequiredService<CapturingConnection>();
		var fromConnection = connection.Captured.GetRequiredService<CredentialSource>();

		fromConnection.Should().NotBeSameAs(fromApp);
		fromConnection.Tokens.Id.Should().NotBe(fromApp.Tokens.Id);

	}

	[Fact]
	public void AScopedConnection_ResolvesTheSameGraphAsTheApplication() {

		var provider = Host(ServiceLifetime.Scoped)
			.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var appScope = provider.CreateScope();

		var fromApp = appScope.ServiceProvider.GetRequiredService<CredentialSource>();
		var connection = appScope.ServiceProvider.GetRequiredService<CapturingConnection>();

		connection.Captured.GetRequiredService<CredentialSource>().Should().BeSameAs(fromApp);

		// And still one connection for the application's life, which is what a browser scope is.
		appScope.ServiceProvider.GetRequiredService<CapturingConnection>().Should().BeSameAs(connection);

	}

}
