# Architecture and development

## Source map

| File under work/connect | Responsibility |
| --- | --- |
| App.cs | Entry point, setup/rollback, shortcuts, settings UI and session lifetime |
| Core.cs | Proxy discovery, transport, TLS record handling, relay, health check, settings, binary preparation |
| UI.cs | Custom rounded buttons, status panel and accessible checkbox rows |
| I18n.cs | RU/EN text dictionary and dynamic status translation |
| Tests.cs | Isolated filesystem, networking and UI-state regression tests |
| audit/AuditRunner.cs | Console test runner with an isolated Store.Root |
| AssemblyInfo.cs | Product metadata and version |
| build.ps1 | Framework compiler invocation, optional audit, executable packaging |
| app.ico, connector.png | Committed icon assets; no font download needed |
| make_icon.py | Optional Pillow-based icon generator; not part of runtime/build |

The compact layout is intentional: no service, browser extension, external daemon or package restore is needed. User-defined desktop/Start menu paths in tests are redirected away from the actual desktop.

## Launch flow

1. Read stored consent and options. Invalid settings default to disabled.
2. Find Antigravity in saved or standard locations. If already running, open the existing app without changing its environment.
3. Acquire a per-session named mutex to avoid duplicate helpers.
4. Enumerate loopback proxy candidates: enabled Windows system proxy; common ports; recognized VPN-core listeners from netstat. Probe HTTP CONNECT / no-auth SOCKS5 to www.google.com:443.
5. Select the first proxy that accepts the probe; otherwise allow a recognized active TUN adapter when enabled.
6. Start an IPv4 loopback relay on an OS-assigned port and prepare compatible Antigravity binaries.
7. Launch with HTTP_PROXY, HTTPS_PROXY, ALL_PROXY (also lowercase), AG_LS_PROXY, NO_PROXY/no_proxy and Chromium proxy flags.
8. Keep relay alive while any Antigravity-named process remains. On exit, dispose the listener and active sockets.

The probe verifies a proxy negotiation, not full endpoint TLS health. Setup and Check VPN additionally perform Health.Check. Route selection currently does not retry a second candidate after a later TLS failure.

## Network behavior

CONNECT traffic to exactly cloudcode-pa.googleapis.com:443 or daily-cloudcode-pa.googleapis.com:443 is opened upstream to www.google.com:443. The original client's SNI and encrypted HTTP contents remain unchanged. The first eligible TLS handshake record is split after 32 payload bytes; all handshake bytes are preserved. Other destinations pass through normally. Plain HTTP forwarding is also implemented for basic requests.

Each tunnel pumps both directions asynchronously, preserves TCP half-close, and closes both ends on error. Dispose closes tracked idle and active clients. Initial network operations have timeouts; established tunnels have no receive timeout. There is no connection cap, local client authentication, VPN kill switch, credential handling, or complete general-purpose HTTP proxy parser. Use it as a local app helper, not a public proxy.

Health.Check uses Windows SslStream with TLS 1.2, normal certificate validation and revocation checking. It sends HEAD / to both Google hosts and accepts an HTTP response even when its status is not 2xx. This establishes transport reachability, not model availability or account authorization.

## Persistence and mutation

Settings and manifests use a temporary file and atomic replacement. Shortcut backups contain original file bytes; installation snapshots changed links plus manifest/settings and restores them on a caught failure. This does not make an OS crash or the entire EXE/icon update transactional. Disable only touches still-owned links or restores tracked removed originals.

Binary preparation scans Antigravity's resources tree for language_server.exe / agy.exe, replaces equal-length ASCII markers, and saves original bytes as SHA-256-named backups. It uses atomic file replacement. Marker matching is heuristic, not a version/signature allowlist. Backups do not include an automatic restoration map; prefer reinstalling Antigravity for a clean rollback. The existing installation may already have been patched by another tool.

The install root is resolved to a physical path to handle packaged Windows launchers. Never commit settings, shortcut manifests, logs, profile data or binary backups.

## Build and test

Run on 64-bit Windows with the .NET Framework compiler at Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe:

```powershell
.\work\connect\build.ps1 -Audit
# Optional custom output:
.\work\connect\build.ps1 -Destination outputs\custom-setup.exe
```

Audit must exit zero and print AUDIT TESTS PASSED. Tests cover exact host matching, patch boundaries/idempotency, TLS byte integrity, truncated streams, HTTP/SOCKS negotiation, strict CONNECT status parsing, half-close, disposing idle clients, corrupt settings, shortcut restoration, failed-install rollback, desktop option migration and persistent RU/EN switching without losing drafts. They use local listeners and temporary test directories; they do not need a live VPN.

Build artifacts and test directories are ignored by Git. The repository includes a Windows CI build/test workflow. CI has no live user VPN or Google login and cannot establish end-to-end model functionality. Fonts fall back from Inter to Segoe UI. Review scaling and keyboard focus on the target Windows display.

## CLI

| Argument | Behavior |
| --- | --- |
| none / --settings | Show settings window |
| --launch | Start or focus Antigravity using stored setup |
| --check | Require stored consent; respect saved TUN setting; test connection; write check-result.txt only on success; exit 0/1 |
| --refresh-shortcuts | Reapply saved setup, including EXE update from the setup binary; exit 0/1 |
| --self-test | Developer entry point; use build.ps1 -Audit instead for safe isolated invocation |

For CLI diagnostics trust the process exit code; an old success file is not cleared after a later failure. Logs contain operational errors, not complete network traces.

## Release procedure

Update AssemblyInfo, changelog and documentation, run the isolated audit, review the file list and Git diff, test the UI, build from the release source tree, calculate SHA-256, then publish the tagged release EXE and checksums. Close installed instances before a local update. No signing certificate or auto-update system is configured. Release files must not contain user data or the reference project's distribution.
