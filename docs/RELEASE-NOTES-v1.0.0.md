# Cirreum.Runtime.RemoteConnections.WebSockets 1.0.0

First release of the app-facing registration surface for Cirreum raw WebSocket remote connections.

## What this is for

`Cirreum.RemoteConnections.WebSockets` supplies the connection; this package supplies the line that
registers it. Without it an application composes the wiring itself — build the context from the
provider, construct the connection, decide its lifetime — the same way in every application, which
makes it exactly the kind of thing that drifts.

## What it provides

**`AddRemoteConnectionFactory<TConnection>()`** is the verb most raw-WebSocket consumers want. A
telephony bridge holds one outbound connection per active call: N concurrent calls, N connections,
each created when the media stream opens and disposed when the call ends.

```csharp
builder.AddRemoteConnectionFactory<RealtimeVoiceConnection>(options => {
    options.EndpointUri = new Uri("wss://provider.example.com/realtime");
});
```

It registers `IRemoteConnectionFactory<TConnection>` and no connection instance, so a status surface
enumerating standing connections never sees per-session ones. Ownership inverts with the lifetime —
the caller creates, connects, and disposes what the factory returns:

```csharp
await using var voice = this._voiceFactory.Create();
await voice.ConnectAsync(ct);
```

`Create` optionally adjusts the registered options for one session — a different deployment, a
per-call subprotocol — leaving the registration untouched for every later one.

**`AddRemoteConnection<TConnection>()`** serves the other lifetime: one connection for as long as
the process runs, such as a standing market-data feed. It resolves as `TConnection` and as
`IRemoteConnection`, and the container disposes it with the host. Registration does not connect.

## Registration rules

**One registration per connection type**, in either shape. Registering the same type twice with
equal options is a no-op; with different options, or under both verbs, it throws. Subclass the
connection to reach a second endpoint.

The registry is keyed by service collection rather than held process-wide. A registry shared across
a process would make a second container — a test host, a second builder — silently skip registrations
the first container had already claimed.

**Options are validated as they are registered.** A missing or relative endpoint surfaces while the
application is composing, not when something first resolves the connection.

## Host neutrality

Both verbs are extension members on `IDomainApplicationBuilder`, which a Blazor WebAssembly builder
and a server-side builder both implement. There is no per-host variant, and no `.Wasm` package.

## Requirements

* `Cirreum.RemoteConnections.WebSockets` 1.0.0 or later, which carries the transport and
  `WebSocketRemoteConnection`. It flows in transitively, along with `Cirreum.Domain`,
  `Cirreum.Contracts` and `Cirreum.Kernel`.
* A host registering `IRemoteConnectionTokenSource` if connections are to present the session
  credential without configuring one — `Cirreum.Runtime.Wasm` 3.0.0 does.
