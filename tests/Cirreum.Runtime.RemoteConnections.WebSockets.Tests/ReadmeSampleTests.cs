namespace Cirreum.Runtime.RemoteConnections.WebSockets.Tests;

using Cirreum;
using Cirreum.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

/// <summary>
/// Compiles the connection type and the registrations the README documents. A sample that no
/// longer matches the surface fails the build here rather than at a reader.
/// </summary>
public class ReadmeSampleTests {

	// README — "Write the connection type"
	public sealed class RealtimeVoiceConnection(WebSocketRemoteConnectionContext context)
		: WebSocketRemoteConnection(context) {

		public event Func<ReadOnlyMemory<byte>, Task>? AudioReceived;

		public Task SendAudioAsync(ReadOnlyMemory<byte> audio, CancellationToken ct = default) =>
			this.SendBytesAsync(audio, WebSocketMessageType.Binary, ct);

		protected override async ValueTask OnFrameReceivedAsync(
			ReadOnlyMemory<byte> payload, WebSocketMessageType messageType, CancellationToken ct) {

			if (messageType == WebSocketMessageType.Binary) {
				await (this.AudioReceived?.Invoke(payload) ?? Task.CompletedTask);
				return;
			}

			await base.OnFrameReceivedAsync(payload, messageType, ct);

		}

	}

	// README — "Application-lifetime connections"
	public sealed class MarketDataConnection(WebSocketRemoteConnectionContext context)
		: WebSocketRemoteConnection(context);

	private sealed class SampleBuilder : IDomainApplicationBuilder {

		public IServiceCollection Services { get; } = new ServiceCollection();

		public ILoggingBuilder Logging => throw new NotSupportedException();

	}

	[Fact]
	public void The_documented_registrations_compile_and_resolve() {

		var builder = new SampleBuilder();

		// README — "Per-session connections"
		builder.AddRemoteConnectionFactory<RealtimeVoiceConnection>(options => {
			options.EndpointUri = new Uri("wss://provider.example.com/realtime");
		});

		// README — "Application-lifetime connections"
		builder.AddRemoteConnection<MarketDataConnection>(options => {
			options.EndpointUri = new Uri("wss://feed.example.com/stream");
		});

		builder.Services.Should().Contain(d => d.ServiceType == typeof(IRemoteConnectionFactory<RealtimeVoiceConnection>));
		builder.Services.Should().Contain(d => d.ServiceType == typeof(IRemoteConnection));

	}

}
