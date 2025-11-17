# WinTrimmy

A Windows system tray utility that automatically flattens multi-line shell command snippets copied to the clipboard, enabling them to paste and run as single-line commands.

This is a Windows port of [Trimmy](https://github.com/steipete/trimmy) for macOS.

## Features

- **Automatic Processing**: Monitors clipboard every 150ms and automatically flattens multi-line commands
- **Smart Detection**: Uses heuristics to identify shell commands vs regular text
- **Aggressiveness Levels**:
  - **Low**: Conservative - only obvious multi-line commands
  - **Normal**: Balanced approach for typical use cases
  - **High**: Aggressive flattening of most multi-line text
- **Box Drawing Removal**: Cleans up terminal border characters (│)
- **Blank Line Preservation**: Optionally preserve blank lines for readability
- **Manual Trim**: Double-click tray icon or use menu to force trim
- **Launch at Login**: Start automatically with Windows
- **Balloon Notifications**: Get notified when clipboard is trimmed
- **PowerShell-aware**: Understands common PowerShell verbs and caret/backtick continuations in addition to Unix-style commands

## Requirements

- Windows 10/11
- .NET 8.0 Runtime

## Building

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Publish as self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true
```

## Development setup & prerequisites

These steps save you from the “why doesn’t this compile/run” rabbit hole we already hit:

- **Windows is required to run or test the app.** The project targets `net8.0-windows` with WinForms. Linux/macOS machines can edit the code, but they cannot build/run without the Windows Desktop SDK (which Microsoft only ships on Windows).
- **Install the .NET 8 SDK + WindowsDesktop workload.** Either install Visual Studio 2022 with “.NET desktop development”, or run:
  ```powershell
  winget install Microsoft.DotNet.SDK.8
  ```
  Verify with `dotnet --list-sdks` – you should see an `8.0.x` entry in addition to any 9.0 preview you might have.
- **Use the solution file.** All commands (`dotnet build`, `dotnet test`) should reference `WinTrimmy.sln` so both the app and xUnit project get built with the same SDK.
- **Running tests:** `dotnet test WinTrimmy.sln` (or `.\\WinTrimmy.Tests\\WinTrimmy.Tests.csproj`) executes the heuristic parity suite. Make sure you’re on Windows so the WindowsDesktop SDK is available.
- **Global clones inside WSL/Linux:** Restoring is fine, but `dotnet build`/`test` will fail because the WindowsDesktop targets aren’t available. Do CI or manual test runs from Windows.
- **Tray icon (.ico) note:** The WinForms tray icon is drawn programmatically, so there is no `trimmy.ico` in the repo. If you want a custom icon, drop `trimmy.ico` next to `WinTrimmy.csproj` and add `<ApplicationIcon>trimmy.ico</ApplicationIcon>` back into the project file.
- **Copy/paste tips:** The detector now handles Unix `\` continuations and PowerShell caret/backtick pipelines. If you still need literal multi-line PS scripts to remain untouched, temporarily toggle Auto-Trim from the tray menu.
- **Template engine permissions:** On locked-down environments (e.g., WSL), dotnet templates try to write under `~/.templateengine`. If that path is read-only, set `DOTNET_CLI_HOME=.` when running `dotnet new`/`dotnet sln`.

## Installation

1. Build or download the release
2. Run `WinTrimmy.exe`
3. The scissors icon appears in the system tray
4. Right-click to configure settings

## Usage

1. Copy a multi-line command from documentation or a website:
   ```
   docker run -d \
     --name mycontainer \
     -p 8080:80 \
     -v /data:/app/data \
     myimage:latest
   ```

2. WinTrimmy automatically flattens it to:
   ```
   docker run -d --name mycontainer -p 8080:80 -v /data:/app/data myimage:latest
   ```

3. Paste directly into your terminal!

## Configuration

Settings are stored in `%LOCALAPPDATA%\WinTrimmy\settings.json`

- **Auto-trim enabled**: Toggle automatic clipboard monitoring
- **Aggressiveness**: How aggressively to detect commands (Low/Normal/High)
- **Preserve blank lines**: Keep intentional blank lines in output
- **Remove box drawing**: Strip terminal border characters
- **Launch at login**: Start with Windows (via Registry)

## How It Works

1. Polls clipboard every 150ms for changes
2. When text is detected, scores it using heuristics:
   - Line continuations (`\`)
   - Pipe characters (`|`)
   - Command chaining (`&&`, `;`, `||`)
   - Common command prefixes (`sudo`, `git`, `docker`, etc.)
   - Flag patterns (`--verbose`, `-v`)
3. If score exceeds threshold, flattens the text
4. Writes the flattened result back to clipboard
5. Shows notification and updates status

## Credits

Based on [Trimmy](https://github.com/steipete/trimmy) by [@steipete](https://github.com/steipete)

## License

MIT License
