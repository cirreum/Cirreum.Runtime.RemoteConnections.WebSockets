namespace Cirreum.Runtime.RemoteConnections.WebSockets.Tests;

using Cirreum;
using Cirreum.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class RemoteConnectionRegistrationTests {

	// ---------------------------------------------------------------------
	// Harness
	// ---------------------------------------------------------------------

	private sealed class TestBuilder : IDomainApplicationBuilder {

		public IServiceCollection Services { get; } = new ServiceCollection();

		public ILoggingBuilder Logging => throw new NotSupportedException();

	}

	private sealed class TestConnection(WebSocketRemoteConnectionContext context) : WebSocketRemoteConnection(context) {

		public Uri Endpoint { get; } = context.Options.EndpointUri;

		public IReadOnlyList<string> Scopes { get; } = context.Options.Scopes;

	}

	private sealed class OtherConnection(WebSocketRemoteConnectionContext context) : WebSocketRemoteConnection(context);

	private const string HubUri = "wss://api.example.com/realtime";

	private static Action<RemoteConnectionOptions> Endpoint(string uri) {
		return options => options.EndpointUri = new Uri(uri);
	}

	// ---------------------------------------------------------------------
	// Registration
	// ---------------------------------------------------------------------

	[Fact]
	public async Task AddRemoteConnection_resolves_as_the_connection_type_and_as_IRemoteConnection() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		await using var provider = builder.Services.BuildServiceProvider();

		var connection = provider.GetRequiredService<TestConnection>();
		var asContract = provider.GetRequiredService<IRemoteConnection>();

		// A status surface enumerating IRemoteConnection must see the same instance the
		// application injects by its concrete type, not a second connection.
		asContract.Should().BeSameAs(connection);

	}

	[Fact]
	public async Task AddRemoteConnection_registers_one_instance_for_the_life_of_the_container() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		await using var provider = builder.Services.BuildServiceProvider();

		provider.GetRequiredService<TestConnection>()
			.Should().BeSameAs(provider.GetRequiredService<TestConnection>());

	}

	[Fact]
	public void AddRemoteConnection_does_not_construct_the_connection_until_it_is_resolved() {

		var builder = new TestBuilder();

		// Registration must not build a transport connection.
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		builder.Services.Should().Contain(d => d.ServiceType == typeof(TestConnection));

	}

	[Fact]
	public void AddRemoteConnection_rejects_a_relative_endpoint_at_registration() {

		var builder = new TestBuilder();

		// A misconfiguration surfaces while the application composes, not when something
		// first resolves the connection.
		var act = () => builder.AddRemoteConnection<TestConnection>(
			options => options.EndpointUri = new Uri("/realtime", UriKind.Relative));

		act.Should().Throw<InvalidOperationException>().WithMessage("*absolute*");

	}

	[Fact]
	public void AddRemoteConnection_rejects_a_missing_endpoint_at_registration() {

		var builder = new TestBuilder();

		var act = () => builder.AddRemoteConnection<TestConnection>(_ => { });

		act.Should().Throw<InvalidOperationException>().WithMessage("*EndpointUri*");

	}

	// ---------------------------------------------------------------------
	// One registration per connection type
	// ---------------------------------------------------------------------

	[Fact]
	public void Registering_the_same_type_twice_with_equal_options_is_a_no_op() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		builder.Services.Count(d => d.ServiceType == typeof(TestConnection)).Should().Be(1);
		builder.Services.Count(d => d.ServiceType == typeof(IRemoteConnection)).Should().Be(1);

	}

	[Fact]
	public void Registering_the_same_type_with_different_options_throws() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		var act = () => builder.AddRemoteConnection<TestConnection>(Endpoint("wss://api.example.com/realtime-admin"));

		act.Should().Throw<InvalidOperationException>().WithMessage("*different options*");

	}

	[Fact]
	public void Registering_the_same_type_as_both_a_connection_and_a_factory_throws() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		var act = () => builder.AddRemoteConnectionFactory<TestConnection>(Endpoint(HubUri));

		act.Should().Throw<InvalidOperationException>().WithMessage("*cannot also be registered*");

	}

	[Fact]
	public void Different_connection_types_register_independently() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(Endpoint(HubUri));
		builder.AddRemoteConnection<OtherConnection>(Endpoint("wss://api.example.com/realtime-admin"));

		builder.Services.Count(d => d.ServiceType == typeof(IRemoteConnection)).Should().Be(2);

	}

	[Fact]
	public void A_second_builder_registers_its_own_connections() {

		var first = new TestBuilder();
		first.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		// A registry held process-wide would make this second composition — a test host, a
		// second builder — silently skip a registration the first container had claimed.
		var second = new TestBuilder();
		second.AddRemoteConnection<TestConnection>(Endpoint(HubUri));

		second.Services.Should().Contain(d => d.ServiceType == typeof(TestConnection));

	}

	// ---------------------------------------------------------------------
	// Per-session factory
	// ---------------------------------------------------------------------

	[Fact]
	public void AddRemoteConnectionFactory_registers_no_connection_instance() {

		var builder = new TestBuilder();
		builder.AddRemoteConnectionFactory<TestConnection>(Endpoint(HubUri));

		// A status surface enumerating standing connections must not see per-session ones.
		builder.Services.Should().NotContain(d => d.ServiceType == typeof(IRemoteConnection));
		builder.Services.Should().NotContain(d => d.ServiceType == typeof(TestConnection));
		builder.Services.Should().Contain(d => d.ServiceType == typeof(IRemoteConnectionFactory<TestConnection>));

	}

	[Fact]
	public async Task The_factory_creates_a_distinct_instance_per_call() {

		var builder = new TestBuilder();
		builder.AddRemoteConnectionFactory<TestConnection>(Endpoint(HubUri));

		await using var provider = builder.Services.BuildServiceProvider();
		var factory = provider.GetRequiredService<IRemoteConnectionFactory<TestConnection>>();

		factory.Create().Should().NotBeSameAs(factory.Create());

	}

	[Fact]
	public async Task A_per_instance_adjustment_does_not_reach_later_instances() {

		var builder = new TestBuilder();
		builder.AddRemoteConnectionFactory<TestConnection>(Endpoint(HubUri));

		await using var provider = builder.Services.BuildServiceProvider();
		var factory = provider.GetRequiredService<IRemoteConnectionFactory<TestConnection>>();

		// Had Create mutated the registration, the endpoint set for this one session would
		// become the endpoint every later session connects to.
		factory.Create(options => options.EndpointUri = new Uri("wss://api.example.com/realtime-session"));

		var subsequent = factory.Create();

		subsequent.Endpoint.Should().Be(new Uri(HubUri));

	}

	// ---------------------------------------------------------------------
	// What the factory's per-instance copy carries
	// ---------------------------------------------------------------------

	[Fact]
	public async Task A_factory_created_instance_carries_the_registered_scopes() {

		// The factory copies the registered options per instance. A property the copy forgets is
		// silently lost, and for scopes that means a connection whose credential source is never
		// told what audience to mint for.
		var builder = new TestBuilder();
		builder.AddRemoteConnectionFactory<TestConnection>(options => {
			options.EndpointUri = new Uri(HubUri);
			options.Scopes = ["api://contoso/access_as_user"];
		});

		await using var provider = builder.Services.BuildServiceProvider();
		var created = provider.GetRequiredService<IRemoteConnectionFactory<TestConnection>>().Create();

		created.Scopes.Should().Equal("api://contoso/access_as_user");

	}

	[Fact]
	public async Task A_registered_connection_carries_the_registered_scopes() {

		var builder = new TestBuilder();
		builder.AddRemoteConnection<TestConnection>(options => {
			options.EndpointUri = new Uri(HubUri);
			options.Scopes = ["api://contoso/access_as_user"];
		});

		await using var provider = builder.Services.BuildServiceProvider();

		provider.GetRequiredService<TestConnection>().Scopes
			.Should().Equal("api://contoso/access_as_user");

	}

	[Fact]
	public void Registering_the_same_factory_type_with_different_options_throws() {

		var builder = new TestBuilder();
		builder.AddRemoteConnectionFactory<TestConnection>(Endpoint(HubUri));

		var act = () => builder.AddRemoteConnectionFactory<TestConnection>(Endpoint("wss://api.example.com/realtime-admin"));

		act.Should().Throw<InvalidOperationException>().WithMessage("*different options*");

	}

}
