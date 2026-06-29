# Artalk.Xmpp

Artalk.Xmpp is a .NET 10 XMPP client library for connecting to XMPP servers, sending and receiving messages, managing presence and rosters, and using common XMPP extension protocols.

## Supported Runtime

- .NET 10: `Artalk.Xmpp`
- .NET 10: `Artalk.ExtendedClient` sample
- .NET 10 Windows: `Artalk.WinClient` sample

The core library targets `net10.0` and does not require Windows-only packages.

## Supported XMPP Features

- TCP XML streams
- STARTTLS
- SASL authentication: PLAIN, DIGEST-MD5, SCRAM-SHA-1
- Instant messaging and presence
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

Reference the project directly:

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

## Security Notes

STARTTLS now uses the platform certificate validator by default. If a server requires custom certificate validation, pass a `RemoteCertificateValidationCallback` to the client constructor.

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
