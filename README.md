# Artalk.Xmpp

[NuGet: Artalk.Xmpp](https://www.nuget.org/packages/Artalk.Xmpp) - 145,510 total downloads as of 2026-07-28

Artalk.Xmpp is a .NET 10 XMPP client library for connecting to XMPP servers, sending and receiving messages, managing presence and rosters, and using common XMPP extension protocols.

## Supported Runtime

- .NET 10: `Artalk.Xmpp`
- .NET 10: `Artalk.ExtendedClient` sample
- .NET 10 Windows: `Artalk.WinClient` sample

The core library targets `net10.0` and does not require Windows-only packages.

## Supported XMPP Features

- RFC 6120 TCP XML streams
- RFC 7622-oriented JID parsing, IDN domain normalization, and PRECIS-profile checks
- RFC 7590 TLS recommendations with RFC 6120 STARTTLS and direct TLS support
- XEP-0124 / XEP-0206 XMPP over BOSH
- RFC 7395 XMPP over WebSocket
- RFC 6120 SASL authentication: OAUTHBEARER, CISCO-VTG-TOKEN, SCRAM-SHA3-512-PLUS, SCRAM-SHA3-512, SCRAM-SHA-512-PLUS, SCRAM-SHA-512, SCRAM-SHA-384-PLUS, SCRAM-SHA-384, SCRAM-SHA-256-PLUS, SCRAM-SHA-256, SCRAM-SHA-224-PLUS, SCRAM-SHA-224, SCRAM-SHA-1-PLUS, SCRAM-SHA-1, DIGEST-MD5, PLAIN
- XEP-0388 SASL2 foundation: stream-feature parsing, SASL2 authentication framing, additional-data handling, and no post-success stream restart
- XEP-0386 Bind 2 resource binding over SASL2 with `authorization-identifier` parsing
- XEP-0440 SASL Channel-Binding Type Capability for SCRAM-PLUS selection
- XEP-0474 SASL SCRAM Downgrade Protection hash verification
- XEP-0480 SASL Upgrade Tasks for SCRAM salted-password hash upgrades over SASL2
- XEP-0515 TLS Channel-Binding Downgrade Protection for SCRAM over TCP TLS
- XEP-0453 DOAP project metadata: [`doap.xml`](doap.xml)
- RFC 6120 optional legacy XMPP session establishment
- RFC 6121 instant messaging and presence
- RFC 6121 roster management
- XEP-0045 Multi-User Chat basics: join, leave, groupchat messages, and occupant presence
- XEP-0030 Service Discovery
- XEP-0115 Entity Capabilities
- XEP-0199 XMPP Ping
- XEP-0085 Chat State Notifications
- OMEMO foundation: XEP-0384 device list, bundle, trust/session orchestration, encrypted envelope, and payload crypto helpers
- OMEMO media sharing: XEP-0454 `aesgcm://` URIs, AES-256-GCM media encryption, strict body parsing, and JPEG thumbnails
- XEP-0084 User Avatar, XEP-0107 User Mood, XEP-0118 User Tune, and XEP-0108 User Activity
- XEP-0077 In-Band Registration
- XEP-0049 Private XML Storage
- XEP-0004 Data Forms and XEP-0020 Feature Negotiation for legacy XEP-0095 / XEP-0096 file-transfer flows
- XEP-0065 SOCKS5 Bytestreams and XEP-0047 In-Band Bytestreams
- XEP-0092 Software Version, XEP-0202 Entity Time, XEP-0224 Attention, XEP-0231 Bits of Binary, and XEP-0279 Server IP Check
- XEP-0191 Blocking Command

## Install

Install the NuGet package:

```powershell
dotnet add package Artalk.Xmpp --version 2.20.0
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

## Cisco VTG Token Authentication

For Cisco Unified CM IM and Presence or Cisco Jabber deployments that advertise SASL `CISCO-VTG-TOKEN`, set `Username` and `CiscoVtgToken` before connecting:

```csharp
using Artalk.Xmpp.Client;

using var client = new ArtalkXmppClient("cisco-im.example.com");

client.Username = "user@example.com";
client.CiscoVtgToken = ciscoToken;
client.Connect("jabber-client");
```

When `CiscoVtgToken` is set and the server advertises `CISCO-VTG-TOKEN`, Artalk.Xmpp selects it before password-based SASL mechanisms. The library sends the caller-provided token through Cisco's SASL mechanism; it does not acquire Cisco SSO or edge-authentication tokens.

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

Cisco VTG tokens are sent only when the server advertises `CISCO-VTG-TOKEN`. Use STARTTLS, direct TLS, BOSH over HTTPS, or WebSocket over WSS when authenticating with Cisco token-based mechanisms.

OMEMO support covers current XEP-0384 device list and bundle publication/retrieval, trust policies, trust-store integration, encrypted message envelope parsing/serialization, payload encryption/authentication, and send/decrypt orchestration. Applications must provide a vetted `IOmemoSessionCipher` adapter and persistent OMEMO key/session store for per-device X3DH and Double Ratchet state.

OMEMO media sharing support covers XEP-0454 `aesgcm://` URL creation/parsing, AES-256-GCM encryption/decryption, appended authentication tags, strict message-body parsing, and optional JPEG thumbnails. Do not display `aesgcm://` links as browser-openable URLs; the URI fragment contains encryption key material and must stay inside the OMEMO-encrypted message flow. Only HTTPS download/upload URLs are accepted when creating media URIs.

SCRAM `-PLUS` mechanisms are preferred automatically on encrypted TCP XMPP streams when the server advertises them and a remote certificate is available. Artalk.Xmpp understands XEP-0440 `sasl-channel-binding` announcements and uses SCRAM-PLUS only when `tls-server-end-point` is mutually supported, while preserving compatibility with servers that have not implemented XEP-0440 yet. The .NET `SslStream` API does not currently expose the TLS Finished messages needed for `tls-unique` or TLS exporter keying material needed for `tls-exporter`, so those binding types are not advertised by Artalk.Xmpp yet.

When a SCRAM server-first-message includes the XEP-0474 `h` attribute, Artalk.Xmpp verifies it against the hash of the SASL mechanism list and XEP-0440 channel-binding type list that were actually advertised for the active SASL profile. A mismatch fails authentication before the client proof is sent, protecting SCRAM and SCRAM-PLUS negotiation from active downgrade tampering.

When a SCRAM server-first-message includes the XEP-0515 `t` attribute on a TCP TLS stream, Artalk.Xmpp compares it with the negotiated `SslStream` TLS version encoded as four lowercase hexadecimal characters such as `0303` for TLS 1.2 and `0304` for TLS 1.3. A mismatch fails authentication before the client proof is sent. BOSH and WebSocket transports do not expose the negotiated TLS version through the current .NET transport APIs, so XEP-0515 verification is only available for TCP TLS and STARTTLS streams.

When an encrypted stream advertises XEP-0388 SASL2, Artalk.Xmpp uses the SASL2 `<authentication/>` profile, sends initial responses inside `<initial-response/>`, verifies SCRAM server signatures from `<additional-data/>`, and waits for the authenticated `<stream:features/>` without restarting the stream.

When the same SASL2 profile advertises XEP-0386 Bind 2, Artalk.Xmpp includes an inline `<bind xmlns='urn:xmpp:bind:0'/>` request in `<authenticate/>`, uses the `Connect(resource)` value as the optional Bind 2 `<tag/>`, and reads the final full JID from `<authorization-identifier/>`. If Bind 2 is not advertised, the client falls back to the legacy RFC 6120 resource bind IQ.

If the SASL2 feature advertises XEP-0480 SCRAM upgrade tasks and password authentication is available, Artalk.Xmpp requests supported `UPGR-SCRAM-*` tasks, handles the SASL2 `<continue/>` flow, derives the requested salted password from the server-provided salt and iteration count, and returns it inside `<task-data><hash/></task-data>`. Upgrade tasks that are not SCRAM hash upgrades remain unsupported and fail with a clear authentication error.

When a server advertises legacy XMPP session establishment, Artalk.Xmpp completes it. Modern servers that omit the legacy session feature are no longer rejected during sign-in.

Automatic UPnP port mapping is disabled in the .NET 10 cross-platform build because the previous implementation depended on a Windows COM component. File transfer still supports SOCKS5, configured proxies, in-band bytestreams, and STUN-based address discovery.

## Avatar API

The avatar extension no longer exposes `System.Drawing.Image`. Avatar events now provide raw bytes and MIME type through `AvatarChangedEventArgs.AvatarData` and `AvatarChangedEventArgs.MimeType`, which keeps the library usable in services, containers, and non-Windows environments.

## Build And Test

```powershell
dotnet build .\Artalk.XmppSoloution.sln
dotnet test .\Artalk.XmppSoloution.sln
```

## Project Metadata

Artalk.Xmpp publishes XEP-0453-compatible DOAP metadata in [`doap.xml`](doap.xml). This RDF/XML file describes the project, repository, NuGet package, and implemented XMPP RFC/XEP support for software listings and automated XMPP ecosystem indexes.

### RFC Support

| RFC | Area | Support |
| --- | --- | --- |
| [RFC 6120](https://datatracker.ietf.org/doc/html/rfc6120) | XMPP Core | partial |
| [RFC 6121](https://datatracker.ietf.org/doc/html/rfc6121) | Instant Messaging and Presence | partial |
| [RFC 7395](https://datatracker.ietf.org/doc/html/rfc7395) | XMPP over WebSocket | complete |
| [RFC 7590](https://datatracker.ietf.org/doc/html/rfc7590) | TLS in XMPP | partial |
| [RFC 7622](https://datatracker.ietf.org/doc/html/rfc7622) | XMPP Address Format | partial |

### XEP Support

The DOAP metadata includes the current XEP document version, the first Artalk.Xmpp 2.x package version that exposed the support, implementation status, and notes for each XEP.

| XEP | Spec Version | Since | Status | Notes |
| --- | --- | --- | --- | --- |
| [XEP-0004](https://xmpp.org/extensions/xep-0004.html) | 2.13.2 | 2.0.0 | partial | Data Forms are used by the registration and feature-negotiation helpers. |
| [XEP-0020](https://xmpp.org/extensions/xep-0020.html) | 1.6 | 2.0.0 | partial | Deprecated feature-negotiation helpers remain for legacy stream-initiation and file-transfer flows. |
| [XEP-0030](https://xmpp.org/extensions/xep-0030.html) | 2.5.0 | 2.0.0 | partial | Service discovery query helpers are available. |
| [XEP-0045](https://xmpp.org/extensions/xep-0045.html) | 1.35.5 | 2.2.0 | partial | Basic room join, leave, groupchat message, and occupant presence support. |
| [XEP-0047](https://xmpp.org/extensions/xep-0047.html) | 2.0.1 | 2.0.0 | partial | In-band bytestream support is available for file-transfer flows. |
| [XEP-0049](https://xmpp.org/extensions/xep-0049.html) | 1.2 | 2.0.0 | complete | Private XML storage get/set helpers are available. |
| [XEP-0065](https://xmpp.org/extensions/xep-0065.html) | 1.8.2 | 2.0.0 | partial | SOCKS5 bytestream and STUN-assisted address discovery helpers are available. |
| [XEP-0077](https://xmpp.org/extensions/xep-0077.html) | 2.4 | 2.0.0 | partial | In-band registration form flow is supported. |
| [XEP-0084](https://xmpp.org/extensions/xep-0084.html) | 1.1.4 | 2.0.0 | partial | User avatar publication and change events expose raw avatar data and MIME type. |
| [XEP-0085](https://xmpp.org/extensions/xep-0085.html) | 2.1 | 2.0.0 | complete | Chat state notification helpers are available. |
| [XEP-0092](https://xmpp.org/extensions/xep-0092.html) | 1.1 | 2.0.0 | complete | Software version replies are available. |
| [XEP-0095](https://xmpp.org/extensions/xep-0095.html) | 1.2 | 2.0.0 | partial | Deprecated stream initiation support remains for legacy file-transfer flows. |
| [XEP-0096](https://xmpp.org/extensions/xep-0096.html) | 1.3.1 | 2.0.0 | partial | Deprecated SI file-transfer support remains for legacy interoperability. |
| [XEP-0107](https://xmpp.org/extensions/xep-0107.html) | 1.2.2 | 2.0.0 | partial | User mood publication/parsing helpers are available. |
| [XEP-0108](https://xmpp.org/extensions/xep-0108.html) | 1.3 | 2.0.0 | partial | User activity publication/parsing helpers are available. |
| [XEP-0115](https://xmpp.org/extensions/xep-0115.html) | 1.6.0 | 2.0.0 | partial | Entity capabilities helpers are available. |
| [XEP-0118](https://xmpp.org/extensions/xep-0118.html) | 1.3.0 | 2.0.0 | partial | User tune publication/parsing helpers are available. |
| [XEP-0124](https://xmpp.org/extensions/xep-0124.html) | 1.11.2 | 2.5.0 | complete | BOSH session open and restart support are available. |
| [XEP-0163](https://xmpp.org/extensions/xep-0163.html) | 1.2.2 | 2.0.0 | partial | PEP is used by the avatar and OMEMO helpers. |
| [XEP-0191](https://xmpp.org/extensions/xep-0191.html) | 1.3 | 2.0.0 | partial | Simplified blocking command support is available. |
| [XEP-0199](https://xmpp.org/extensions/xep-0199.html) | 2.0.1 | 2.3.0 | complete | Server ping and keepalive flows are available. |
| [XEP-0202](https://xmpp.org/extensions/xep-0202.html) | 2.0 | 2.0.0 | complete | Entity time replies are available. |
| [XEP-0206](https://xmpp.org/extensions/xep-0206.html) | 1.4 | 2.5.0 | complete | XMPP over BOSH transport is available. |
| [XEP-0224](https://xmpp.org/extensions/xep-0224.html) | 1.0 | 2.0.0 | complete | Attention messages are available. |
| [XEP-0231](https://xmpp.org/extensions/xep-0231.html) | 1.1 | 2.0.0 | partial | Bits of Binary helpers are available. |
| [XEP-0279](https://xmpp.org/extensions/xep-0279.html) | 0.2 | 2.0.0 | complete | Server IP check support is available. |
| [XEP-0384](https://xmpp.org/extensions/xep-0384.html) | 0.9.1 | 2.8.0 | partial | XMPP-facing OMEMO device, bundle, envelope, payload, trust, and session orchestration helpers are available; applications provide the session cipher implementation. |
| [XEP-0386](https://xmpp.org/extensions/xep-0386.html) | 1.1.0 | 2.20.0 | partial | Bind 2 discovery, SASL2 inline bind request, optional tag, and authorization-identifier parsing are supported; linked inline session features remain future work. |
| [XEP-0388](https://xmpp.org/extensions/xep-0388.html) | 1.0.4 | 2.15.0 | partial | SASL2 authentication framing and SCRAM additional-data verification are supported. |
| [XEP-0440](https://xmpp.org/extensions/xep-0440.html) | 1.0.0 | 2.14.0 | complete | SASL channel-binding type discovery is supported. |
| [XEP-0453](https://xmpp.org/extensions/xep-0453.html) | 0.1.2 | 2.19.1 | complete | The project publishes RDF/XML DOAP metadata. |
| [XEP-0454](https://xmpp.org/extensions/xep-0454.html) | 0.1.0 | 2.13.0 | partial | OMEMO media URI, AES-GCM media encryption, encrypted length, and message-body helpers are available. |
| [XEP-0474](https://xmpp.org/extensions/xep-0474.html) | 0.5.0 | 2.17.0 | complete | SCRAM downgrade-protection hash verification is supported. |
| [XEP-0480](https://xmpp.org/extensions/xep-0480.html) | 0.2.0 | 2.16.0 | partial | SCRAM salted-password hash upgrade tasks are supported. |
| [XEP-0515](https://xmpp.org/extensions/xep-0515.html) | 0.1.0 | 2.18.0 | complete | TLS version downgrade-protection verification is supported for TCP TLS streams. |

## License

This project is released under the MIT License.

## Donate

Artalk.Xmpp is maintained as an open-source .NET/XMPP library. If this project saves you time, helps a production integration, or gives you a cleaner starting point for XMPP work, a donation helps keep the library maintained, tested, documented, and published on NuGet.

[![Donate with PayPal](https://img.shields.io/badge/Donate-PayPal-00457C?style=for-the-badge&logo=paypal)](https://www.paypal.com/ncp/payment/QSWNJM49EA2FG)

You can also donate directly through this PayPal payment link: [Donate to Artalk.Xmpp](https://www.paypal.com/ncp/payment/QSWNJM49EA2FG).
