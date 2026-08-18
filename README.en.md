# Informer

[RU Русский](README.md) | [EN English](README.en.md)

A system-tray application that receives notifications over HTTP (JSON) from external
systems (1C, POS software, scripts, third-party services), displays them as popup toasts,
and keeps a searchable history filterable by sender. Completely free, open source (MIT).

Cross-platform (Windows / Linux / macOS), with a multi-language interface and the ability
to add new languages without rebuilding.

---

## Download a ready-made build

No need to build anything yourself — ready-made installers for all three OSes are on the
project's **[Releases](https://github.com/rbsoft-dev/Informer/releases)** page. Download the
installer for your system (`.exe` for Windows, `.deb` for Linux, `.pkg` for macOS) and run
it — the [Publishing for Windows / Linux / macOS](#publishing-for-windows--linux--macos)
section below explains installation on each OS in more detail.

Building the project from source (sections below) is only needed for developers who want to
change something in it.

---

## Table of contents

- [Download a ready-made build](#download-a-ready-made-build)
- [Features](#features)
- [Stack](#stack)
- [Solution structure](#solution-structure)
- [How to open and build in Visual Studio 2022](#how-to-open-and-build-in-visual-studio-2022)
- [How to open and build in JetBrains Rider](#how-to-open-and-build-in-jetbrains-rider)
- [How to open and build in VS Code](#how-to-open-and-build-in-vs-code)
- [Applying migrations](#applying-migrations-before-first-run)
- [Running](#running)
- [Publishing for Windows / Linux / macOS](#publishing-for-windows--linux--macos)
- [Building installers](#building-installers)
- [Settings](#settings)
- [Localization](#localization)
- [API — incoming request format](#api--incoming-request-format)
- [Other endpoints](#other-endpoints)
- [Security](#security)
- [Troubleshooting](#troubleshooting)
- [License and authorship](#license-and-authorship)

---

## Features

- **HTTP notification intake** — any system able to send a `POST` request with JSON can
  become a notification source (1C, POS software, scripts, webhooks)
- **A single reusable toast**, not a "tower" of windows — new notifications don't interrupt
  what you're currently reading, unless it's the very first one to arrive. Page through with
  the ◀ ▶ arrows, a 🔔 "bell" to jump to the latest one, a "Show full message" button for
  long text (the window smoothly grows upward, staying on screen instead of running off the
  bottom edge)
- **Notification history** — a full list filterable by sender, manual "read" marking,
  deletion, a "new messages" indicator that scrolls to the freshest entries
- **Tray icon badge** — the unread count is shown right on the icon (Slack/Gmail-style) and
  in the "Notification History" menu item itself
- **Settings** — history retention period, server port, API key requirement, anti-spam rate
  limit, toast display duration, display policy by severity, interface language
- **Localization** — translations are stored as plain text `.po` files; the language list is
  built automatically from whatever's on disk; a new language can be downloaded via a direct
  link right from the Settings window, without reinstalling the app
- **"About" window** — developer info, license, built-in documentation on usage and
  integration
- **Cross-platform** — self-contained builds for Windows, Linux, and macOS (Intel and Apple
  Silicon)

## Stack

- **.NET 6** (net6.0)
- **Avalonia UI 11** — cross-platform interface + system tray (`TrayIcon`)
- **ASP.NET Core Minimal API (Kestrel)**, embedded in the same process as the UI
- **EF Core 6 + SQLite** — the provider is wired up in one place (`Program.cs`), so switching
  to SQL Server/PostgreSQL is a one-line change (`UseSqlite(...)`)
- **Karambolo.PO** — reads translations from `.po` files (gettext format)
- **Microsoft.Extensions.Configuration.Ini** — the official configuration provider used to
  read `lang.ini` (display names for the language picker)

## Solution structure

```
Informer.sln
├── Informer.Core   — entities (Notification, ApiKeyEntity, AppSettingsEntity), DTOs, NotificationBus
├── Informer.Data   — InformerDbContext, migrations, design-time factory
├── Informer.Api    — middleware (API key, rate limit), minimal API endpoints
└── Informer.App    — Avalonia UI (tray, toasts, history, settings, "About") + the Program.cs
                       entry point, which brings up Kestrel and the UI loop in a single
                       process, + translations (Localization/langs/*.po, lang.ini)
```

## How to open and build in Visual Studio 2022

### Step 1 — check your Visual Studio 2022 version

Any VS2022 edition works (Community is free and sufficient). If it's not installed yet,
download it from [visualstudio.microsoft.com](https://visualstudio.microsoft.com/downloads/).

If VS2022 is already installed, check for updates just in case: **Help → Check for
Updates**. An outdated version sometimes handles newer SDKs/NuGet packages worse.

### Step 2 — install the required workload

1. Open **Visual Studio Installer** (find it in the Start menu, separate from VS itself —
   it's a standalone installer utility)
2. Find your VS2022 installation → **"Modify"** button
3. Under the **"Workloads"** tab, find and check:
   **".NET desktop development"**
   — without it, VS can't build a project with `OutputType=WinExe` (the type of our
   Informer.App)
4. The **"Avalonia for Visual Studio"** Marketplace extension is **not required** to build
   — it's only needed if you want a live XAML preview right in the editor (a visual designer
   for `.axaml` files). Without it, everything still builds and runs exactly the same, just
   without the live interface preview

### Step 3 — check that the .NET 6 SDK is installed

Open PowerShell:
```powershell
dotnet --list-sdks
```
A line with version `6.0.x` should be in the list. If it's missing, it's usually installed
automatically together with the workload from Step 2, but if it's still absent, install it
separately:
```powershell
winget install Microsoft.DotNet.SDK.6
```

### Step 4 — open the solution

Double-click `Informer.sln`, or in VS2022: **File → Open → Project/Solution**.

### Step 5 — set the startup project

The solution consists of 4 projects (`Informer.Core`, `Informer.Data`, `Informer.Api`,
`Informer.App`), but the one you need to run is `Informer.App` — it's the only executable
project (the other three are libraries with no entry point of their own).

In **Solution Explorer** (usually on the right): find `Informer.App`, **right-click → "Set
as Startup Project"**. Its name will become **bold** in the list afterward — confirmation
that everything's set correctly.

### Step 6 — NuGet package restore

This usually happens **automatically** on the first build — VS downloads all packages
listed in the `.csproj` files (Avalonia, EF Core, Karambolo.PO,
Microsoft.Extensions.Configuration.Ini, and the rest) as long as "Allow NuGet to download
missing packages" is checked (enabled by default).

If the build complains about missing packages (or after switching branches or leaving the
project untouched for a while) — restore manually:
**Right-click the "Informer" solution in Solution Explorer → "Restore NuGet Packages"**

Restoring requires internet access (packages are downloaded from nuget.org) — if building
on a machine without internet, you'll need to set up a local NuGet cache/mirror in advance.

### Step 7 — build and run

`Ctrl+Shift+B` (Build Solution) — the first build may take longer (compiling Avalonia
markup, downloading packages), subsequent ones are faster.

`F5` — build and immediately run (or the green "Play" button at the top labeled
`Informer.App`). The app will appear in the system tray — no window will open, that's
expected (it's a tray app by design).

### Common issues at this stage

- **"Workload '.NET desktop development' not found"** — go back to Step 2, verify the
  checkbox is actually set and applied (VS may ask for a restart after installation)
- **Errors about missing types/namespaces right after cloning** — almost always fixed with
  **Build → Clean Solution**, then **Build → Rebuild Solution** (a full rebuild from
  scratch, not an incremental one)
- **"Restore failed" / NuGet timeout** — check your internet connection, or manually clear
  the cache: `dotnet nuget locals all --clear`, then retry the restore

## How to open and build in JetBrains Rider

### Step 1 — install Rider

Download from [jetbrains.com/rider](https://www.jetbrains.com/rider/) — a paid product, but
a 30-day free trial is available; it's also free for non-commercial use under certain
conditions (students, open-source maintainers — check the current terms on the JetBrains
site).

### Step 2 — check the .NET 6 SDK

```powershell
dotnet --list-sdks
```
If version `6.0.x` is missing, install it (Rider will itself offer to install the needed
SDK the first time you open the solution, if it detects a mismatch).

### Step 3 — Avalonia plugin (optional, but recommended)

Unlike VS2022, Rider has a full official **"Avalonia"** plugin from the JetBrains
Marketplace — gives you a live XAML preview, style/resource autocompletion, file templates.
Install via **File → Settings → Plugins → Marketplace → search "Avalonia"**.

### Step 4 — open the solution

**File → Open** → point it at `Informer.sln`. Rider will recognize the solution structure
(all 4 projects) on its own and automatically start restoring NuGet packages, no extra
steps needed.

### Step 5 — run configuration

Top-right corner — a run configuration dropdown. Rider usually **creates one on its own**
for every executable project when opening the solution — pick **Informer.App** from the
list.

If there's no configuration in the list (rare) — create one manually:
**Run → Edit Configurations → "+" → .NET Project → Project: Informer.App**

### Step 6 — build and run

- Build: `Ctrl+F9` (or **Build → Build Solution**)
- Run with debugging: `Shift+F10` (or the green "Run" button next to the configuration)
- Run without debugging: `Ctrl+F5`

### Common issues

- **NuGet issues** — right-click the solution in **Solution Explorer → Restore NuGet
  Packages**, or `Tools → NuGet → Restore`
- **Odd behavior after switching branches / long idle periods** — **File → Invalidate
  Caches...** (Rider's equivalent of "clear cache and rebuild" in VS2022), then restart
  Rider

## How to open and build in VS Code

VS Code isn't a full IDE with a built-in solution model like VS2022/Rider — it's an
extensible text editor. You can work with it two ways: minimally (via the terminal, no
special extensions) or with an extension that gives a more "IDE-like" experience.

### Option A — just use the terminal (the most reliable, no surprises)

1. Install [VS Code](https://code.visualstudio.com/) and the
   [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. **File → Open Folder** → point it at the repository root (where `Informer.sln` lives)
3. Open the built-in terminal (`` Ctrl+` ``), run commands directly:
   ```powershell
   dotnet restore
   dotnet build
   dotnet run --project Informer.App
   ```
   That's enough for the full development cycle — edit code in VS Code (basic C#/XAML
   syntax highlighting works out of the box), build and run via the terminal. Simple,
   predictable, nothing can "break" because of extension quirks.

### Option B — with the C# Dev Kit extension (a more familiar IDE experience)

1. Install the **"C# Dev Kit"** extension (`ms-dotnettools.csdevkit`) from the Marketplace
   inside VS Code — it automatically pulls in the base C# extension and a .NET Runtime
   install tool
2. **Important note**: C# Dev Kit requires signing in with a **free** Microsoft account
   (a regular account, not a corporate subscription) — you'll be prompted to authenticate
   the first time you use it; this is normal and expected, not a bug
3. After installing, open the folder containing `Informer.sln` — a **Solution Explorer**
   panel will appear in the sidebar (similar to the one in VS2022), showing the structure of
   all 4 projects
4. **Right-click `Informer.App` → "Set as Startup Project"**
5. Build — `Ctrl+Shift+B`, run/debug — `F5` (the extension will automatically offer to
   create a `launch.json`; accept the suggested configuration)

### About XAML preview in VS Code

Unlike Rider, VS Code has **no** full official Avalonia plugin with a live interface
preview — `.axaml` files are edited as plain XML/text with syntax highlighting, no visual
designer. If a live interface preview matters to your workflow, Rider or VS2022 are better
suited for that.

### Common issues

- **IntelliSense not working / "can't see" types** — usually fixed by restarting the
  OmniSharp/C# Dev Kit server: `Ctrl+Shift+P` → "Restart Language Server" (or just restart
  VS Code itself)
- **`dotnet restore` can't find packages** — same cause as in VS2022/Rider (no internet, or
  a corrupted cache): `dotnet nuget locals all --clear`, then retry

## Applying migrations (before first run)

Open **Tools → NuGet Package Manager → Package Manager Console**, select
**Default project: Informer.Data**, then:

```powershell
Add-Migration InitialCreate -Project Informer.Data -StartupProject Informer.App
Update-Database -Project Informer.Data -StartupProject Informer.App
```

`Program.cs` calls `db.Database.Migrate()` automatically on startup, so after running
`Update-Database` once (to generate the migration file), just launching the exe on other
machines is enough — the database creates/updates itself.

Future migrations after entity changes use the same `Add-Migration <Name>` +
`Update-Database` pair.

## Running

`F5` in VS2022. The app will appear in the system tray. Right-click the icon:
**Notification History**, **Settings**, **About**, **Exit**.

On first run:
- The interface language is auto-detected from the OS's language (if a matching `.po` file
  is installed — otherwise the first available one is used)
- The server port defaults to **`4399`** (configurable in Settings, requires an app restart
  to take effect)
- Kestrel listens on `0.0.0.0` (all network interfaces) — see the [Security](#security)
  section

## Publishing for Windows / Linux / macOS

From `Informer.App`:

```powershell
# Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux (x64)
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS Intel
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# macOS Apple Silicon (M1/M2/M3)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

Result — in `Informer.App\bin\Release\net6.0\<rid>\publish\`.

### Running on Linux

```bash
chmod +x Informer
./Informer
```

### Running on macOS

```bash
chmod +x Informer
xattr -d com.apple.quarantine ./Informer
./Informer
```
(`xattr` removes the Gatekeeper quarantine flag for unsigned apps — without this, macOS
will refuse to run the file via double-click or the console until explicitly allowed in
System Settings)

## Building installers

Besides a "plain" publish (`dotnet publish`, see above), the repository has ready-made
scripts for building full installers for each OS — with shortcuts, an uninstaller, system
registration, etc.

### Layout

```
installers/
├── for-windows/    — Informer.iss (Inno Setup script)
├── for-linux/      — deb-template/ (.deb structure) + build-deb.sh
├── for-macos-intel/ and for-macos-m/  — app-template/ (Info.plist) + build-pkg.sh
                       (Intel and Apple Silicon — use the same build-pkg.sh, the only
                       difference is which publish output you pass it: osx-x64 or
                       osx-arm64)

dist/               — where the FINISHED installer files land after a local build:
├── windows/Informer-Setup-1.0.0.exe
├── linux/informer_1.0.0_amd64.deb
└── macos/Informer.pkg
```

`installers/` holds the source/scripts for **building an installer yourself** from source.
`dist/` is the result of that build on your machine — **gitignored**, not stored in the
repository.

**Ready-made installers for end users are published via GitHub Releases** — see the
[Download a ready-made build](#download-a-ready-made-build) section above. How to publish a
new version (uploading files, creating a release) is described in the
[project Wiki](https://github.com/rbsoft-dev/Informer/wiki), page **"How to make project
releases"**.

### Windows — Inno Setup

Install [Inno Setup](https://jrsoftware.org/isdl.php) (6.x or newer), open
`installers/for-windows/Informer.iss`, **Build → Compile** (`Ctrl+F9`). A fresh publish is
needed before building (`dotnet publish -r win-x64 ...`, see above) — the script pulls
files from there.

Installs **without administrator rights** (into `%LocalAppData%\Programs\Informer` — not
`Program Files`, since the app writes `informer.db`/`crash.log` next to the exe, and
`Program Files` isn't writable by a regular user).

### Linux — `.deb` package

```bash
cd installers/for-linux
chmod +x build-deb.sh
./build-deb.sh /path/to/publish
```

Before building, check the current `libicu` package name on your target distro
(`apt-cache search libicu`) and fix the `Depends:` line in
`deb-template/DEBIAN/control` if it differs from the default listed there.

Install **strictly** via `apt install ./informer_1.0.0_amd64.deb` (not `dpkg -i`) — only
that way will `apt` automatically pull in any missing dependencies.

### macOS — `.pkg`

Run **only on an actual Mac** (needs `iconutil`/`pkgbuild` from the Xcode Command Line
Tools: `xcode-select --install`).

```bash
cd installers/for-macos-intel   # or for-macos-m — for Apple Silicon
chmod +x build-pkg.sh
./build-pkg.sh /path/to/publish
```

The package is **unsigned** (no Apple Developer certificate — $99/year, which this project
doesn't have) — the user will need to explicitly allow it once via **System Settings →
Privacy & Security → "Open Anyway"**.

## Settings

All settings are stored in a single row of the `AppSettings` table (SQLite) and apply
**immediately** after saving — with one exception:

| Setting | Applies | Note |
|---|---|---|
| Interface language | Instantly | Doesn't require the "Save" button |
| History retention period | After "Save" | A background service purges records older than N days |
| Server port | **After restarting the app** | Kestrel can't rebind its port on the fly |
| Require API key | After "Save" | Only protects `/api/notify` |
| Anti-spam (requests / per how many seconds) | After "Save" | Counted per IP address |
| Toast display duration | After "Save" | In seconds, until auto-close |
| Display policy (Regular/Warnings/Errors) | After "Save" | Only controls toast popups — always visible in history |
| API keys | Instantly | Creation/revocation/deletion apply right away |

## Localization

Translations are stored as plain text `.po` files (gettext format) in
`Informer.App/Localization/langs/`. The language list in Settings is built
**automatically** — by scanning that folder, not from a hardcoded list in the code.

### How to add a new language

1. Copy `en.po`, rename it to the language's code (`de.po`, `es.po`, ...) — use the
   **real** ISO 639-1 code (`de`, not `ger`; `zh`, not `ch`) — both the OS-language
   auto-detection and the fallback display name depend on it
2. Translate the `msgstr "..."` strings — easiest with [Poedit](https://poedit.net) (a free
   `.po` editor, no coding knowledge needed) or any text editor **saved as UTF-8**
3. (Optional) add an explicit display name to
   `Informer.App/Localization/langs/lang.ini`:
   ```ini
   de = Deutsch
   ```
   Without this line, the display name is determined automatically via .NET's built-in
   culture database — an explicit entry is only needed for non-standard codes or to
   control the exact spelling/capitalization

### Downloading a language pack without rebuilding

In Settings, in the "Interface language" card, there's a field for a direct link to a
`.po` file — once downloaded, the language immediately appears in the list and is applied,
with no reinstall required.

### Already translated languages

`ru` (Russian), `en` (English), `fr` (Français), `zh` (中文, Simplified).

## API — incoming request format

```
POST http://<address>:4399/api/notify
Content-Type: application/json
X-Api-Key: <key, if required in settings>

{
  "header": "1C:Session:MainBase:ivanov",
  "description": "New document posted",
  "type": "info",
  "ResponseBody": {
    "any": "arbitrary structure"
  }
}
```

- `header` — displayed as the sender, used for filtering in history; not involved in
  authorization
- `description` — the message text
- `type` — optional field: `info` (default), `warning`, or `error` — affects the toast's
  border color and whether it pops up at all (per the "Display policy" in Settings); always
  visible in history regardless of the policy
- `ResponseBody` — an arbitrary JSON structure, stored as-is

Authorization is only via the `X-Api-Key` header, checked against active keys from
`Settings → API keys`. Required only if "Require API key" is enabled in Settings.

### Quick test with curl

```bash
curl -X POST http://127.0.0.1:4399/api/notify \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <key_from_settings_window>" \
  -d '{"header":"TestSender","description":"Hello from curl","type":"info","ResponseBody":{"x":1}}'
```

## Other endpoints

Used by the UI itself, but also available externally for debugging/integrations. **Not
protected by the API key** — see [Security](#security).

- `GET    /api/history?sender=...&fromUtc=...&toUtc=...&page=1&pageSize=100`
- `GET    /api/history/senders`
- `POST   /api/history/{id}/read`
- `GET    /api/settings`
- `PUT    /api/settings`
- `GET    /api/apikeys`
- `POST   /api/apikeys`
- `DELETE /api/apikeys/{id}`

## Security

- By default, Kestrel listens on `0.0.0.0` (all network interfaces), not just
  `127.0.0.1` — this allows the API to be reached from other devices on the same local
  network
- **`/api/notify`** — protected by the API key (if enabled in Settings) and a per-IP rate
  limit
- **`/api/history` and `/api/settings` have no authorization at all.** With `0.0.0.0`
  exposed on an untrusted network, anyone on that network can read the history and change
  settings (including creating/revoking API keys) without a password. Fine for a trusted
  home/office LAN; don't do this on a public or unfamiliar network
- The bind address can be changed via `appsettings.json` → `Kestrel:BindAddress`
  (`"127.0.0.1"` — local only, `"0.0.0.0"` — whole network)

## Troubleshooting

- **`crash.log`** (next to the exe) — a log of unhandled exceptions. Written automatically
  at startup (`AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`), and
  wraps potentially risky event handlers (showing a toast, the "About" window, etc.) so a
  failure in one spot doesn't silently hang the whole app
- If the app won't launch on Linux/macOS — check execute permissions
  (`chmod +x Informer`) and, on macOS, removal of the quarantine flag
  (`xattr -d com.apple.quarantine`)
- If Cyrillic or other non-ASCII text shows up as "�" — check the file's encoding
  (`.po`, `lang.ini`) — it must be **UTF-8**, not "ANSI"

## License and authorship

Freely distributed under the **MIT** license — see the [`LICENSE`](LICENSE) file. Free to
use, modify, and embed in your own projects, including commercial ones, as long as the
license text is retained in copies.

**Developer:** Evgeniy Ershov / RBSoft
**Website:** [rbsoft.ru](https://rbsoft.ru)
**Email:** [online@rbsoft.ru](mailto:online@rbsoft.ru)
**Telegram:** [@rbsoft_official](https://t.me/rbsoft_official)
