# Artalk.Xmpp

[NuGet: Artalk.Xmpp](https://www.nuget.org/packages/Artalk.Xmpp) - 135,617 total downloads as of 2026-07-02

Artalk.Xmpp is a .NET 10 XMPP client library for connecting to XMPP servers, sending and receiving messages, managing presence and rosters, and using common XMPP extension protocols.

## Supported Runtime

- .NET 10: `Artalk.Xmpp`
- .NET 10: `Artalk.ExtendedClient` sample
- .NET 10 Windows: `Artalk.WinClient` sample

The core library targets `net10.0` and does not require Windows-only packages.

## Supported XMPP Features

- TCP XML streams
- STARTTLS and direct TLS
- XMPP over BOSH
- SASL authentication: OAUTHBEARER, SCRAM-SHA3-512-PLUS, SCRAM-SHA3-512, SCRAM-SHA-512-PLUS, SCRAM-SHA-512, SCRAM-SHA-384-PLUS, SCRAM-SHA-384, SCRAM-SHA-256-PLUS, SCRAM-SHA-256, SCRAM-SHA-224-PLUS, SCRAM-SHA-224, SCRAM-SHA-1-PLUS, SCRAM-SHA-1, DIGEST-MD5, PLAIN
- Optional legacy XMPP session establishment
- Instant messaging and presence
- Multi-user chat basics: join, leave, groupchat messages, and occupant presence
- Roster management
- Service discovery
- Entity capabilities
- XMPP ping
- Chat state notifications
- OMEMO foundation: XEP-0384 device list, bundle, trust/session orchestration, encrypted envelope, and payload crypto helpers
- User avatar, mood, tune, and activity
- In-band registration
- Private XML storage
- SOCKS5 and in-band file transfer
- Simplified blocking

## Install

Install the NuGet package:

```powershell
dotnet add package Artalk.Xmpp --version 2.10.0
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

## BOSH

For servers that expose XMPP over BOSH, pass the BOSH connection manager URL and the XMPP service domain:

```csharp
using Artalk.Xmpp.Client;

var boshUrl = new Uri("https://chat.example.com/http-bind");

using var client = new ArtalkXmppClient(
    boshUrl,
    "example.com",
    "myusername",
    "mypassword");

client.Connect("bosh-client");
client.SendMessage("room-user@example.com", "Hello over BOSH");
```

## OAuth Bearer Authentication

For servers that advertise SASL `OAUTHBEARER`, set `Username` and `OAuthBearerToken` before connecting:

```csharp
using Artalk.Xmpp.Client;

using var client = new ArtalkXmppClient("xmpp.example.com");

client.Username = "myusername";
client.OAuthBearerToken = accessToken;
client.Connect("oauth-client");
```

When `OAuthBearerToken` is set and the server advertises `OAUTHBEARER`, Artalk.Xmpp prefers it over password-based SASL mechanisms.

## OMEMO Foundation

Artalk.Xmpp includes the XMPP-facing OMEMO layer for XEP-0384: device list and bundle PEP nodes, trust decisions, session orchestration, encrypted message envelopes, payload encryption/authentication, and high-level send/decrypt helpers. It uses the current `urn:xmpp:omemo:2` namespace.

```csharp
using Artalk.Xmpp.Client;
using Artalk.Xmpp.Extensions;

using var client = new ArtalkXmppClient("example.com", "myusername", "mypassword");

client.OmemoDeviceListChanged += (sender, e) => {
    Console.WriteLine($"{e.Jid} devices: {string.Join(", ", e.DeviceList.DeviceIds)}");
};

client.Connect("omemo-device");

uint deviceId = 123456;
client.PublishOmemoDeviceList(new uint[] { deviceId });

var bundle = new OmemoBundle(
    signedPreKeyId: 1,
    signedPreKey: signedPreKeyPublicBytes,
    signedPreKeySignature: signedPreKeySignatureBytes,
    identityKey: identityKeyBytes,
    preKeys: new Dictionary<uint, byte[]> {
        { 1, preKeyPublicBytes }
    });

client.PublishOmemoBundle(deviceId, bundle);

OmemoDeviceList contactDevices = client.GetOmemoDeviceList("friend@example.com");
OmemoBundle contactBundle = client.GetOmemoBundle(
    "friend@example.com",
    contactDevices.DeviceIds[0]);
```

For message encryption, provide an `IOmemoSessionCipher` implementation that adapts your vetted OMEMO-compatible X3DH and Double Ratchet engine. Artalk.Xmpp handles device discovery, trust policy enforcement, payload encryption, envelope construction, sending, parsing, and payload decryption:

```csharp
using System.Text;

IOmemoSessionCipher sessionCipher = CreateSignalProtocolAdapter();
var trustStore = new MemoryOmemoTrustStore();

trustStore.SetTrust(
    "friend@example.com",
    contactDevices.DeviceIds[0],
    contactBundle.IdentityKey,
    OmemoTrustLevel.Trusted);

byte[] plaintext = Encoding.UTF8.GetBytes("hello");

client.SendOmemoMessage(
    "friend@example.com",
    plaintext,
    localDeviceId: deviceId,
    sessionCipher: sessionCipher,
    trustStore: trustStore);
```

`OmemoTrustPolicy.RequireTrusted` is the default. `AllowUndecided` and `TrustOnFirstUse` are available when an application explicitly wants that behavior. Artalk.Xmpp does not bundle a GPL Signal Protocol dependency into the MIT package; instead, `IOmemoSessionCipher` is the adapter boundary for a vetted session implementation and persistent key store.

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

For BOSH, prefer an `https://` connection manager URL so the HTTP binding is protected by TLS.

OAuth bearer tokens are sent only when the server advertises `OAUTHBEARER`. Use TLS or HTTPS transport when authenticating with bearer tokens.

OMEMO support covers current XEP-0384 device list and bundle publication/retrieval, trust policies, trust-store integration, encrypted message envelope parsing/serialization, payload encryption/authentication, and send/decrypt orchestration. Applications must provide a vetted `IOmemoSessionCipher` adapter and persistent OMEMO key/session store for per-device X3DH and Double Ratchet state.

SCRAM `-PLUS` mechanisms are preferred automatically on encrypted TCP XMPP streams when the server advertises them and a remote certificate is available. The current channel binding type is `tls-server-end-point`. The .NET `SslStream` API does not currently expose the TLS Finished messages needed for `tls-unique` or TLS exporter keying material needed for `tls-exporter`, so those binding types are not advertised by Artalk.Xmpp yet.

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
