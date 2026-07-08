# Artalk.Xmpp

[NuGet: Artalk.Xmpp](https://www.nuget.org/packages/Artalk.Xmpp) - 137,762 total downloads as of 2026-07-08

Artalk.Xmpp is a .NET 10 XMPP client library for connecting to XMPP servers, sending and receiving messages, managing presence and rosters, and using common XMPP extension protocols.

## Supported Runtime

- .NET 10: `Artalk.Xmpp`
- .NET 10: `Artalk.ExtendedClient` sample
- .NET 10 Windows: `Artalk.WinClient` sample

The core library targets `net10.0` and does not require Windows-only packages.

## Supported XMPP Features

- TCP XML streams
- RFC 7622-oriented JID parsing, IDN domain normalization, and PRECIS-profile checks
- STARTTLS and direct TLS
- XMPP over BOSH
- XMPP over WebSocket
- SASL authentication: OAUTHBEARER, SCRAM-SHA3-512-PLUS, SCRAM-SHA3-512, SCRAM-SHA-512-PLUS, SCRAM-SHA-512, SCRAM-SHA-384-PLUS, SCRAM-SHA-384, SCRAM-SHA-256-PLUS, SCRAM-SHA-256, SCRAM-SHA-224-PLUS, SCRAM-SHA-224, SCRAM-SHA-1-PLUS, SCRAM-SHA-1, DIGEST-MD5, PLAIN
- XEP-0388 SASL2 foundation: stream-feature parsing, SASL2 authentication framing, additional-data handling, and no post-success stream restart
- XEP-0440 SASL Channel-Binding Type Capability for SCRAM-PLUS selection
- Optional legacy XMPP session establishment
- Instant messaging and presence
- Multi-user chat basics: join, leave, groupchat messages, and occupant presence
- Roster management
- Service discovery
- Entity capabilities
- XMPP ping
- Chat state notifications
- OMEMO foundation: XEP-0384 device list, bundle, trust/session orchestration, encrypted envelope, and payload crypto helpers
- OMEMO media sharing: XEP-0454 `aesgcm://` URIs, AES-256-GCM media encryption, strict body parsing, and JPEG thumbnails
- User avatar, mood, tune, and activity
- In-band registration
- Private XML storage
- SOCKS5 and in-band file transfer
- Simplified blocking

## Install

Install the NuGet package:

```powershell
dotnet add package Artalk.Xmpp --version 2.15.0
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

## JID Normalization

`Jid` normalizes XMPP addresses before comparison and serialization. Domainparts are processed as IDN-aware names, localparts apply RFC 7622-oriented case, width, exclusion, and length checks, and resourceparts are treated as opaque strings with NFC normalization:

```csharp
var jid = new Jid("USER@XN--BCHER-KVA.Example./Phone");

Console.WriteLine(jid.Node);     // user
Console.WriteLine(jid.Domain);   // bücher.example
Console.WriteLine(jid.Resource); // Phone
Console.WriteLine(jid);          // user@bücher.example/Phone
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

## WebSocket

For servers that expose RFC 7395 XMPP over WebSocket, pass the WebSocket endpoint URL, the XMPP service domain, and `XmppTransportBinding.WebSocket`:

```csharp
using Artalk.Xmpp.Client;
using Artalk.Xmpp.Core;

var webSocketUrl = new Uri("wss://chat.example.com/xmpp-websocket");

using var client = new ArtalkXmppClient(
    webSocketUrl,
    "example.com",
    "myusername",
    "mypassword",
    XmppTransportBinding.WebSocket);

client.Connect("websocket-client");
client.SendMessage("friend@example.com", "Hello over WebSocket");
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

## OMEMO Media Sharing

XEP-0454 uses HTTP File Upload for the encrypted bytes and sends only an `aesgcm://` URL inside an OMEMO encrypted message body. `OmemoMediaUri` creates and parses the strict URI form, encrypts media with AES-256-GCM, and appends the authentication tag to the encrypted file as required by the XEP:

```csharp
using System.Text;
using Artalk.Xmpp.Extensions;

Uri uploadUrl = new Uri("https://upload.example.com/files/photo.jpg");
byte[] fileBytes = File.ReadAllBytes("photo.jpg");

OmemoMediaUri mediaUri = OmemoMediaUri.Create(uploadUrl);
byte[] encryptedFile = mediaUri.Encrypt(fileBytes);
long encryptedSize = OmemoMediaUri.GetEncryptedLength(fileBytes.LongLength);

// Upload encryptedFile to uploadUrl. Use encryptedSize when requesting an
// HTTP upload slot for the encrypted payload.

string thumbnail = OmemoMediaMessage.CreateJpegThumbnailDataUri(thumbnailJpegBytes);
string body = new OmemoMediaMessage(mediaUri, thumbnail).ToString();

client.SendOmemoMessage(
    "friend@example.com",
    Encoding.UTF8.GetBytes(body),
    localDeviceId: deviceId,
    sessionCipher: sessionCipher,
    trustStore: trustStore);
```

Receiving applications decrypt the OMEMO message first, parse the media body strictly, download the encrypted HTTPS payload, and then decrypt it with the key and IV from the `aesgcm://` URI:

```csharp
byte[] bodyBytes = client.DecryptOmemoMessage(message, deviceId, sessionCipher);
OmemoMediaMessage media = OmemoMediaMessage.Parse(Encoding.UTF8.GetString(bodyBytes));

byte[] encryptedDownload = Download(media.MediaUri.HttpsUrl);
byte[] originalFile = media.MediaUri.Decrypt(encryptedDownload);
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

For BOSH, prefer an `https://` connection manager URL so the HTTP binding is protected by TLS.

For WebSocket, prefer `wss://` endpoint URLs. RFC 7395 uses the `xmpp` WebSocket subprotocol and sends each stanza as an independent UTF-8 text frame.

OAuth bearer tokens are sent only when the server advertises `OAUTHBEARER`. Use TLS or HTTPS transport when authenticating with bearer tokens.

OMEMO support covers current XEP-0384 device list and bundle publication/retrieval, trust policies, trust-store integration, encrypted message envelope parsing/serialization, payload encryption/authentication, and send/decrypt orchestration. Applications must provide a vetted `IOmemoSessionCipher` adapter and persistent OMEMO key/session store for per-device X3DH and Double Ratchet state.

OMEMO media sharing support covers XEP-0454 `aesgcm://` URL creation/parsing, AES-256-GCM encryption/decryption, appended authentication tags, strict message-body parsing, and optional JPEG thumbnails. Do not display `aesgcm://` links as browser-openable URLs; the URI fragment contains encryption key material and must stay inside the OMEMO-encrypted message flow. Only HTTPS download/upload URLs are accepted when creating media URIs.

SCRAM `-PLUS` mechanisms are preferred automatically on encrypted TCP XMPP streams when the server advertises them and a remote certificate is available. Artalk.Xmpp understands XEP-0440 `sasl-channel-binding` announcements and uses SCRAM-PLUS only when `tls-server-end-point` is mutually supported, while preserving compatibility with servers that have not implemented XEP-0440 yet. The .NET `SslStream` API does not currently expose the TLS Finished messages needed for `tls-unique` or TLS exporter keying material needed for `tls-exporter`, so those binding types are not advertised by Artalk.Xmpp yet.

When an encrypted stream advertises XEP-0388 SASL2, Artalk.Xmpp uses the SASL2 `<authentication/>` profile, sends initial responses inside `<initial-response/>`, verifies SCRAM server signatures from `<additional-data/>`, and waits for the authenticated `<stream:features/>` without restarting the stream. SASL2 continuation tasks such as second-factor or password-change flows are detected and rejected with a clear authentication error until those task XEPs are implemented.

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
