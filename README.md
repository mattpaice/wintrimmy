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
