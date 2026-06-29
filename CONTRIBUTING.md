# Contributing

Thanks for helping improve Artalk.Xmpp.

## Development Setup

Install the current .NET SDK that supports `net10.0`, then restore the solution:

```powershell
dotnet restore .\Artalk.XmppSoloution.sln
```

## Build And Test

Run the full validation before opening a pull request:

```powershell
dotnet build .\Artalk.XmppSoloution.sln -c Release
dotnet test .\Artalk.XmppSoloution.sln -c Release --no-build
dotnet pack .\Artalk.Xmpp\Artalk.Xmpp.csproj -c Release --no-build
```

## Pull Requests

- Keep changes focused on one feature, fix, or documentation update.
- Add tests for protocol parsing, authentication, public API behavior, or bug fixes.
- Update `README.md` and samples when behavior or public API changes.
- Do not include passwords, tokens, server secrets, or private message content in tests or issues.
- Prefer non-breaking API changes unless a major-version update is intentional.

## XMPP Compatibility Reports

When reporting a server compatibility issue, include:

- Artalk.Xmpp version and .NET version.
- Server name/version when possible.
- Host, port, and TLS mode: STARTTLS, direct TLS, or disabled.
- SASL mechanisms advertised by the server, if visible.
- Minimal `SendXml` / `ReceiveXml` traces with credentials and private data removed.

## Release Notes

For package changes, update the package metadata in
`Artalk.Xmpp/Artalk.Xmpp.csproj` and keep the root `README.md` aligned with the
published behavior.
