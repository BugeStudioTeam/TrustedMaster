# TrustedMaster

TrustedMaster is a WinUI 3 front-end for NSudo-style token elevation on Windows.

## Features

- Launch any application as TrustedInstaller, SYSTEM, the current user, or the current process token.
- Optionally enable all privileges on the launched application token.
- Record every successfully authorized application automatically.
- Show only authorized applications on the Permissions page.
- Keep the Modules page empty until it is developed.
- Configure default launch settings from the single Settings entry at the bottom of the navigation pane.
- Add "Run as TrustedMaster" to the Windows file context menu.

## Context menu

TrustedMaster registers a "Run as TrustedMaster" command for files when it starts.

- Authorized applications start immediately with the privileges recorded for them.
- Other applications show an authorization window with the application name, publisher, origin, and target user mode.
- Approving the request launches the application and records it as authorized.
- Packaged installs expose the command in the Windows 11 modern context menu through `Package.appxmanifest`.
- Unpackaged runs register the same COM handler per user and expose the command through "Show more options".

The command handler is a .NET COM host built from `TrustedMaster.ExplorerCommand`. The main project builds it for the selected platform and copies it next to `TrustedMaster.exe`. The handler is framework-dependent, so the .NET 8 runtime must be installed for the context-menu entry to load. When it cannot be loaded, the classic `Show more options` command still works.

## Requirements

- Windows 10 version 1809 or later.
- Run TrustedMaster with administrator privileges.
- .NET 8 runtime for the context-menu handler.
