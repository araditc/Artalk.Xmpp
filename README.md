# Artalk.Xmpp

Artalk.Xmpp is a .NET 10 XMPP client library for connecting to XMPP servers, sending and receiving messages, managing presence and rosters, and using common XMPP extension protocols.

## Supported Runtime

- .NET 10: `Artalk.Xmpp`
- .NET 10: `Artalk.ExtendedClient` sample
- .NET 10 Windows: `Artalk.WinClient` sample

The core library targets `net10.0` and does not require Windows-only packages.

## Supported XMPP Features

- TCP XML streams
- STARTTLS and direct TLS
- SASL authentication: SCRAM-SHA-256, SCRAM-SHA-1, DIGEST-MD5, PLAIN
- Optional legacy XMPP session establishment
- Instant messaging and presence
- Multi-user chat basics: join, leave, groupchat messages, and occupant presence
- Roster management
- Service discovery
- Entity capabilities
- XMPP ping
- Chat state notifications
- User avatar, mood, tune, and activity
- In-band registration
- Private XML storage
- SOCKS5 and in-band file transfer
- Simplified blocking

## Install

Install the NuGet package:

```powershell
dotnet add package Artalk.Xmpp --version 2.3.0
```

Or reference the project directly:

```xml
<ProjectReference Include="..\Artalk.Xmpp\Artalk.Xmpp.csproj" />
```

Or package it locally:

```powershell
dotnet pack .\Artalk.Xmpp\Artalk.Xmpp.csproj -c Release
```

## Quick Start

```csharp
using Artalk.Xmpp.Client;
using Artalk.Xmpp.Im;

var hostname = "example.com";
var username = "myusername";
var password = "mypassword";

using var client = new ArtalkXmppClient(hostname, username, password);

client.Message += (sender, e) => {
    Console.WriteLine($"Message from <{e.Jid}>: {e.Message.Body}");
};

client.Connect("my-resource");
client.SendMessage("friend@example.com", "Hello from Artalk.Xmpp", type: MessageType.Chat);
```

## Direct TLS

Most XMPP servers use STARTTLS on port 5222, which remains the default. For servers that expect TLS immediately after TCP connect, use the `directTls` constructor argument and the server's TLS port, commonly 5223:

```csharp
using var client = new ArtalkXmppClient(
    "xmpp.example.com",
    "myusername",
    "mypassword",
    port: 5223,
    directTls: true);

client.Connect("service");
```

## Presence Tracking

`Connect` retrieves the roster and sends initial presence. Subscribe to `StatusChanged` before connecting to track online, away, and offline updates from contacts:

```csharp
using var client = new ArtalkXmppClient("example.com", "myusername", "mypassword");

client.StatusChanged += (sender, e) => {
    Console.WriteLine($"{e.Jid}: {e.Status.Availability} {e.Status.Message}");
};

client.Connect("presence-sample");
client.SetStatus(Availability.Online, "Ready");
```

## Multi-User Chat

Basic XEP-0045 room participation is available through the high-level client. Join a room with a nickname, receive groupchat messages, track occupant presence, and send room messages:

```csharp
using Artalk.Xmpp.Client;

using var client = new ArtalkXmppClient("example.com", "myusername", "mypassword");

client.RoomMessage += (sender, e) => {
    Console.WriteLine($"[{e.RoomJid}] {e.Nickname}: {e.Message.Body}");
};

client.RoomPresence += (sender, e) => {
    Console.WriteLine($"{e.Occupant.Nickname} available: {e.IsAvailable}");
};

client.Connect("muc-sample");
client.JoinRoom("room@conference.example.com", "my-nickname");
client.SendRoomMessage("room@conference.example.com", "Hello room");
```

## Keepalive And Idle Connections

For long-running clients, use XEP-0199 server pings to detect or prevent idle connection loss:

```csharp
using var client = new ArtalkXmppClient("example.com", "myusername", "mypassword");

client.KeepAliveInterval = TimeSpan.FromMinutes(5);
client.KeepAliveTimeout = TimeSpan.FromSeconds(15);
client.KeepAliveFailed += (sender, e) => {
    Console.WriteLine($"Keepalive failed: {e.Exception.Message}");
};

client.Error += (sender, e) => {
    Console.WriteLine(e.Exception);
};

client.Disconnected += (sender, e) => {
    Console.WriteLine("Disconnected");
};

client.Connect("long-running-client");
```

You can also ping the connected server on demand with `PingServer()`.

## In-Band Registration

Some servers allow XEP-0077 account registration before authenticating. Create the client without credentials, connect, and fill the server-provided form:

```csharp
using Artalk.Xmpp.Client;
using Artalk.Xmpp.Extensions.Dataforms;

using var client = new ArtalkXmppClient("example.com");

client.Connect();
client.Register(form => new SubmitForm(
    new TextField("username", "newuser"),
    new PasswordField("password", "new-password")));
```

## Security Notes

STARTTLS now uses the platform certificate validator by default. If a server requires custom certificate validation, pass a `RemoteCertificateValidationCallback` to the client constructor.

When a server advertises legacy XMPP session establishment, Artalk.Xmpp completes it. Modern servers that omit the legacy session feature are no longer rejected during sign-in.

Automatic UPnP port mapping is disabled in the .NET 10 cross-platform build because the previous implementation depended on a Windows COM component. File transfer still supports SOCKS5, configured proxies, in-band bytestreams, and STUN-based address discovery.

## Avatar API

The avatar extension no longer exposes `System.Drawing.Image`. Avatar events now provide raw bytes and MIME type through `AvatarChangedEventArgs.AvatarData` and `AvatarChangedEventArgs.MimeType`, which keeps the library usable in services, containers, and non-Windows environments.

## Build And Test

```powershell
dotnet build .\Artalk.XmppSoloution.sln
dotnet test .\Artalk.XmppSoloution.sln
```

## License

This project is released under the MIT License.
