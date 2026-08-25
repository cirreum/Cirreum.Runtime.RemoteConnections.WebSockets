# Cirreum.Runtime.RemoteConnections.WebSockets

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.RemoteConnections.WebSockets.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.RemoteConnections.WebSockets/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.RemoteConnections.WebSockets.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.RemoteConnections.WebSockets/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.RemoteConnections.WebSockets?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.RemoteConnections.WebSockets/releases)
[![License](https://img.shields.io/badge/license-MIT-F2F2F2?style=flat-square&labelColor=1F1F1F)](https://github.com/cirreum/Cirreum.Runtime.RemoteConnections.WebSockets/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**App-facing registration for Cirreum raw WebSocket remote connections**

## Overview

**Cirreum.Runtime.RemoteConnections.WebSockets** registers typed raw WebSocket client connections on
a Cirreum application builder, replacing the hand-written factory an application would otherwise
compose. Registration gives the connection framework-owned receive and reconnect loops, credential
refresh, observable state, and disposal.

The transport implementation ships in `Cirreum.RemoteConnections.WebSockets` and flows in
transitively.

## Usage

Write the connection type. It derives from `WebSocketRemoteConnection` and takes the
framework-supplied context as its first constructor parameter. A connection speaking a protocol it
does not own overrides `OnFrameReceivedAsync` and decodes frames itself:

```csharp
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
```

Leaving the seam alone instead gets Cirreum's `{ "method", "payload" }` envelope, dispatched to
handlers registered with `On<T>` — what a Cirreum server writes for a method-addressed push.

### Per-session connections

A telephony bridge holds one outbound connection per active call, so the lifetime belongs to the
session rather than to the application:

```csharp
builder.AddRemoteConnectionFactory<RealtimeVoiceConnection>(options => {
    options.EndpointUri = new Uri("wss://provider.example.com/realtime");
});
```

This registers `IRemoteConnectionFactory<RealtimeVoiceConnection>` and no connection instance. The
caller creates, connects, and disposes what it receives:

```csharp
await using var voice = this._voiceFactory.Create();
await voice.ConnectAsync(ct);
```

`Create` optionally adjusts the registered options for one session — a different deployment, a
per-call subprotocol — leaving the registration untouched for every later one.

### Application-lifetime connections

A single connection that lives as long as the process registers directly:

```csharp
builder.AddRemoteConnection<MarketDataConnection>(options => {
    options.EndpointUri = new Uri("wss://feed.example.com/stream");
});
```

It resolves as `MarketDataConnection` and as `IRemoteConnection`, and the container disposes it with
the host — never dispose an injected connection. Registration does not connect; call `ConnectAsync`
when the caller is ready.

### Notes

- **One registration per connection type**, in either shape. Registering the same type twice with
  equal options is a no-op; with different options, or under both verbs, it throws. Subclass the
  connection to reach a second endpoint.
- **Credentials** resolve from the options when set, and otherwise from the host's ambient
  `IRemoteConnectionTokenSource`. Every connect and reconnect attempt builds a fresh socket and
  re-resolves the credential, because a `ClientWebSocket` cannot be reconnected once closed.
- **The native transport** is reachable through the optional `configureTransport` delegate, which
  receives `ClientWebSocketOptions` for each socket after the framework has configured it — the
  place to offer subprotocols.

## Documentation

- [CHANGELOG](docs/CHANGELOG.md)
- [Backlog](docs/BACKLOG.md)

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Runtime.RemoteConnections.WebSockets follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*