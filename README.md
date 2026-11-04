# Antigravity Connector

**by skam** · Windows · RU / EN · v0.1.9

[Русская документация](README.ru.md) · [English manual](docs/manual.en.md) · [Architecture and development](docs/development.en.md)

A small Windows launcher that starts Antigravity using your connected VPN's local HTTP/SOCKS5 proxy or a recognized system tunnel. One settings window, a persistent language button, and a desktop shortcut. No connector subscription, group key, or bundled VPN service.

![Connector icon](work/connect/connector.png)

## Download and start

1. Install Antigravity and connect your own VPN.
2. Download **AntigravityConnect-Setup.exe** from [Releases](https://github.com/skamcore/antigravity-connector/releases/latest).
3. Open it, allow the connector to use your VPN, and click **Enable**.
4. Next time, use the Antigravity Start menu shortcut, or open **Antigravity Connector** on the desktop and click **Open Antigravity**.

The connector does not start your VPN. It needs to remain connected. Google sign-in is still required. The binary is unsigned; verify the SHA-256 against the release checksum file. Do not disable Windows protection to run an unverified download.

## What it changes

The connector installs into your local application data, backs up and adjusts matching shortcuts, and starts a local relay on an ephemeral loopback port. It supplies proxy settings to the Antigravity process. For two specific Google hosts it changes the upstream destination and splits a TLS handshake record while preserving the original handshake bytes and certificate verification.

It also modifies matching `ineligible` strings in Antigravity's `language_server.exe` / `agy.exe` to `inexigible`, with backups. **Disable restores shortcuts, but does not restore patched Antigravity binaries.** Reinstall Antigravity to return those files to a vendor-provided state. Read the [manual](docs/manual.en.md) before installation.

## Compatibility and limits

- Windows 10/11 with .NET Framework 4.5+ APIs; .NET Framework 4.8 is recommended. Standard Antigravity installation paths only.
- A real Happ proxy connection has been checked; HTTP/SOCKS5 behavior also has local mock tests. Other VPN brands and Windows configurations are not comprehensively validated.
- TUN detection uses adapter names, not proof that traffic exits through a VPN. This is not a VPN kill switch.
- A successful connection check is not proof of account eligibility or a successful model response.
- Restart Antigravity fully after changing VPN routes. Existing application processes keep their current environment.

## Build and review

On Windows, from the repository root:

```powershell
.\work\connect\build.ps1 -Audit
```

The executable is written to `outputs/AntigravityConnect-Setup.exe`. No NuGet packages are required. See [development](docs/development.en.md), [review findings](docs/review.en.md), and [release history](CHANGELOG.md).

## Origin and distribution

Inspired by [confeden/Antigravity](https://github.com/confeden/Antigravity). This repository contains the C# connector implementation and its assets, not that project's source tree, executable, keys, or license. No affiliation with Google or Antigravity is implied. No open-source license is granted by this repository; public source visibility alone is not a redistribution license. See [NOTICE](NOTICE.md).
