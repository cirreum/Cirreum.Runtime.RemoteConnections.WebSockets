# Migration to v1

Initial release — there is no prior version of **Cirreum.Runtime.RemoteConnections.WebSockets** to migrate from.

Applications previously driving `ClientWebSocket` by hand replace that wiring with a single builder call. Connection types derive from `WebSocketRemoteConnection`; frame handling moves onto the derived class through the routing seam.
