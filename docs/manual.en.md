# User manual

## Requirements

Use Windows 10/11 with .NET Framework 4.8 recommended, an installed Antigravity IDE, a Google account with access to the service, and your own working VPN. Python and PowerShell are not needed to run the downloaded EXE. An internet connection is required for connection checks and Antigravity.

The application searches for `Antigravity/Antigravity.exe` under local `Programs`, Program Files, and Program Files (x86). It has no app-path picker. Installs protected by Windows permissions may need additional rights for binary modification; normal per-user installs do not need a service or administrator-level system setup.

## First start

Close Antigravity first, connect the VPN, then run the downloaded setup EXE. Enable **Use my VPN**. Leave **System tunnel** enabled if your VPN uses TUN; local proxy detection is attempted first. Choose whether to create a desktop shortcut, then click **Enable**. The app checks Google connectivity, installs the connector and shortcuts, and launches Antigravity.

If Antigravity is already running, the connector opens that instance; it cannot change its proxy environment retroactively. Fully exit Antigravity and launch it again through the connector to apply routing.

## Controls

| Control | Behavior |
| --- | --- |
| RU / EN | Switches language immediately and remembers it. Unsaved connection options stay unchanged. |
| Use my VPN | Required for enabling the connector and running a UI connection check. To revoke the stored setting, use Disable. |
| System tunnel | Allows recognized active VPN adapters as a fallback when no local proxy works. |
| Desktop shortcut | Creates or removes the connector's own desktop shortcut when options are saved. |
| Enable / Save and enable | Checks connectivity, saves options and installs shortcuts, then launches Antigravity. |
| Open Antigravity | Launches with the saved settings. |
| Check VPN | Tests TLS and HTTP responses from two Google hosts. Does not test a model request. |
| Disable | Restores tracked original shortcuts and revokes saved consent. An existing session is allowed to finish. |

The status **Connector enabled** describes saved configuration, not continuous network monitoring. Changing the consent toggle alone is not an immediate shutdown; use **Disable**.

## Daily use and VPN support

Connect your VPN before opening Antigravity. Use its updated Start menu shortcut, a rewritten existing matching shortcut, or the connector's Open button. Running Antigravity.exe directly bypasses the launcher. This is shortcut integration, not a service that intercepts every possible app launch.

Detection checks the Windows user's enabled loopback system proxy, common local ports, and listeners owned by recognized VPN core process names. Both HTTP CONNECT and SOCKS5 with no authentication are supported. Remote authenticated proxies, PAC scripts, and subscription import are not supported. No subscription or VPN credentials are read.

TUN mode uses ordinary Windows routing. An adapter being active does not prove a full tunnel, correct route, or VPN exit country. Split tunneling can send destinations outside the VPN. There is no kill switch or mid-session route migration. Reconnect the VPN and restart Antigravity if the connection changes.

## Local files and privacy

Default folder: `%LOCALAPPDATA%/Antigravity Connect`. A packaged launcher may redirect this folder; the installed shortcut's target identifies the actual physical location.

| File | Purpose |
| --- | --- |
| AntigravityConnect.exe | Installed application and session relay |
| connector-v4.ico | Shortcut icon |
| settings.json | Consent, tunnel/desktop options, language, Antigravity path |
| shortcuts.json | Original shortcut bytes and restoration metadata |
| connection.log | Local timestamps, route details, failures and session counts |
| check-result.txt | Last successful CLI connection check; can be stale |
| backups/*.bin | Original Antigravity binaries, named by SHA-256 |

No analytics backend, connector account, or automatic upload is implemented. Health checks contact Google. Logs and shortcut backups can contain local paths; review them before sharing. The local relay is bound to IPv4 loopback, has no authentication, and can be used by other processes on the same computer while running. It does not decrypt TLS traffic or install a root certificate.

## Update, disable, remove

Exit Antigravity and all connector windows before updating: the running EXE cannot be replaced. Run the new setup and enable it. There is no automatic updater.

Disable restores shortcuts that are still owned by this connector; unrelated replacements are preserved. Old shortcuts intentionally removed during migration may return if they existed before setup. Disable does not restore patched Antigravity binaries, delete backups, or terminate an active relay session.

To remove: Disable, exit Antigravity and the connector, retain any needed backups, then delete the install folder. To restore Antigravity's original binaries, reinstall it from its official installer. Do not copy an old backup over a newer Antigravity release. Antigravity profiles and Google sign-in data are not deliberately deleted by the connector.

## Troubleshooting

- **VPN not found:** connect it and enable its local HTTP/SOCKS proxy or a recognized TUN mode. The app does not create a VPN connection.
- **EOF / Google setup error:** fully exit Antigravity, reconnect VPN, use Check VPN and relaunch. A network check passing does not validate account access.
- **Compatibility check required:** the expected binary or marker is missing. Stop and review compatibility; arbitrary newer versions are not assumed safe.
- **Access denied / update failed:** close running instances. Confirm the installed location and permissions. The installer cannot overwrite a running executable.
- **Repeated sign-in:** keep the existing Antigravity profile and complete normal sign-in. The connector cannot supply an account or guarantee session retention.
- **Wrong shortcut / missing icon:** reopen the current setup and save settings; use the shortcut targeting AntigravityConnect.exe. Explorer may cache old icons.

For an issue report, include connector/Windows/Antigravity versions, VPN client and mode, exact error, and sanitized log excerpts. Do not include passwords, tokens, subscriptions or full profile directories.
