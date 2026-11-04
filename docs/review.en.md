# Release review — v0.1.8

Follow-up v0.1.9: clean English Windows CI exposed a Cyrillic shortcut save failure in WScript.Shell. Shortcut.cs now uses Unicode IShellLinkW/IPersistFile; the Cyrillic filename regression test remains enabled. UTF-8 compiler input is explicit. One local relay test timed out transiently during repeated runs; the subsequent full audit passed. This transient is not evidence of universal network reliability.

Date: 2026-11-04. Scope: C# source, installer, network relay, translations, tests, build script and publication contents. This is a code review and regression check, not a guarantee of defect-free software or a security certification.

## Fixed in this review

- Saving Desktop=false now removes an existing owned connector shortcut, includes that removal in rollback, and supports enabling it again. An unrelated same-named shortcut is not removed.
- CLI --check requires stored VPN consent and uses the saved AllowTunnel value instead of unconditionally enabling tunnel fallback.
- Added a small by skam attribution without changing the connection flow.
- Successful Disable clears old shortcut metadata so the next enable cycle backs up the user's current shortcuts. A regression test covers an externally changed original.

## Validation

The isolated audit passed locally on Windows. It covers protocol handling, TLS byte preservation, relay lifecycle, installer rollback, shortcut migration and language persistence. The release is also built from the curated publication tree. No user settings, logs, tokens, Antigravity binary backups or reference-project executables are included.

Prior manual testing established a working Happ proxy path and both Google endpoint transport checks. This release does not claim a freshly tested model response, support for every VPN brand, or verified routing for every TUN adapter. Remote CI results are visible in the repository Actions tab.

## Remaining limitations

- EXE updates require closing running connector instances; there is no staged updater.
- TUN is detected heuristically; traffic can follow a non-VPN route. No kill switch.
- Route selection stops at the first successful proxy negotiation; later TLS failures do not trigger candidate fallback.
- An already running Antigravity instance keeps its old environment. Launching its EXE directly bypasses the helper.
- Binary patch compatibility is marker-based and backups are not automatically restored by Disable.
- Established relay connections have no idle timeout or connection cap. The relay trusts local processes.
- Local logs are not rotated. CLI success files can become stale; trust the exit code.
- Network and permission errors from Windows can retain the operating system's language.

These are documented release limits, not claims that all remaining issues have been found.
