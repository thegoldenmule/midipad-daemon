# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MidiPadDaemon is a macOS background daemon written in C# (.NET 10) that translates MIDI events from an Alesis Strike Multipad into keyboard events and other actions. It includes an embedded web server for live configuration.

## Build and Run Commands

```bash
# Restore packages
dotnet restore

# Build the entire solution
dotnet build

# Run the web host + MIDI services (listens on http://localhost:5005)
dotnet run --project MidiPadDaemon.App/MidiPadDaemon.App.csproj

# Publish for Apple Silicon
dotnet publish MidiPadDaemon.App/MidiPadDaemon.App.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true

# Publish for Intel
dotnet publish MidiPadDaemon.App/MidiPadDaemon.App.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true
```

## Architecture

### Project Structure

```
MidiPadDaemon/
├── MidiPadDaemon.slnx              # Solution file (.NET 10 XML format)
├── MidiPadDaemon.App/              # ASP.NET Core minimal API host (entry point)
│   ├── Program.cs                  # DI setup, Kestrel config, endpoint mapping
│   ├── Endpoints/
│   │   ├── ConfigEndpoints.cs      # GET/PUT /config, profiles
│   │   ├── BindingsEndpoints.cs    # CRUD /bindings
│   │   └── DiagnosticsEndpoints.cs # /status, /devices, /test-action, /keys
│   └── Services/
│       └── MidiBackgroundService.cs # IHostedService for MIDI listening
├── MidiPadDaemon.Core/             # Business logic
│   ├── Models/                     # MidiInputEvent, ActionConfig, MidiBinding, etc.
│   ├── Interfaces/                 # IMidiInputService, IMappingEngine, IConfigStore, IActionExecutor
│   ├── Services/
│   │   ├── MidiInputService.cs     # managed-midi wrapper
│   │   ├── MappingEngine.cs        # MIDI event → action dispatch
│   │   └── JsonConfigStore.cs      # Config persistence
│   ├── Executors/
│   │   ├── KeyboardActionExecutor.cs
│   │   ├── ShellCommandActionExecutor.cs
│   │   └── HttpActionExecutor.cs
│   └── Serialization/              # JSON serialization options
└── MidiPadDaemon.Interop/          # macOS platform interop
    ├── Interfaces/
    │   ├── IKeyCodeMapper.cs
    │   └── IKeyboardInjector.cs
    ├── Native/
    │   ├── CoreGraphicsInterop.cs  # P/Invoke for CGEvent
    │   ├── CGEventFlags.cs
    │   └── CGEventTapLocation.cs
    ├── KeyCodeMapper.cs            # Key name → macOS virtual key code
    └── KeyboardInjector.cs         # CGEvent keyboard injection
```

### Data Flow

```
Alesis Pad Hit → managed-midi → MidiInputService → MappingEngine → ActionExecutors
                                                                    ├─ KeyboardActionExecutor (CGEvent)
                                                                    ├─ ShellCommandActionExecutor (disabled by default)
                                                                    └─ HttpActionExecutor
```

### Key Components

- **MidiInputService** (`Core/Services/`) - Wraps managed-midi library, discovers MIDI devices, emits `MidiInputEvent`. Auto-connects to Alesis/Strike devices.
- **MappingEngine** (`Core/Services/`) - Thread-safe lookup from MIDI events to bindings, dispatches to executors. Subscribes to config changes.
- **ActionExecutors** (`Core/Executors/`) - Implementations for keyboard (CGEvent P/Invoke), shell commands (disabled by default), HTTP requests
- **JsonConfigStore** (`Core/Services/`) - JSON persistence at `~/.midi-pad-daemon/config.json`, hot-reloadable, file permissions set to 0600
- **KeyboardInjector** (`Interop/`) - CGEvent-based keyboard injection with modifier key support

### Web API (Kestrel)

Bound to localhost only at `http://127.0.0.1:5005`:

| Endpoint | Description |
|----------|-------------|
| `GET /` | API info and endpoint list |
| `GET /config` | Current configuration |
| `PUT /config` | Replace entire configuration |
| `GET /config/profiles` | List all profiles |
| `PUT /config/profiles/active?profileId=xxx` | Set active profile |
| `GET /bindings` | List bindings in active profile |
| `GET /bindings/{id}` | Get specific binding |
| `POST /bindings` | Create new binding |
| `PUT /bindings/{id}` | Update binding |
| `DELETE /bindings/{id}` | Delete binding |
| `GET /diagnostics/status` | Daemon status, uptime, last event |
| `GET /diagnostics/devices` | List available MIDI devices |
| `POST /diagnostics/test-action/{bindingId}` | Test execute a binding |
| `GET /diagnostics/keys` | List available key names for keyboard actions |

### Configuration

Config file: `~/.midi-pad-daemon/config.json`

Example binding (pad 37 → Enter key):
```json
{
  "activeProfileId": "default",
  "profiles": [{
    "id": "default",
    "name": "Default Profile",
    "bindings": [{
      "id": "pad1-enter",
      "deviceId": "any",
      "channel": 1,
      "eventType": "noteOn",
      "number": 37,
      "valueMatch": null,
      "actions": [{
        "type": "keyboard",
        "key": "Enter",
        "command": false,
        "option": false,
        "control": false,
        "shift": false
      }]
    }]
  }]
}
```

### Action Types

1. **Keyboard** (`type: "keyboard"`) - Inject keystrokes
   - `key`: Key name (e.g., "Enter", "Space", "F1", "A")
   - `command`, `option`, `control`, `shift`: Modifier keys

2. **Shell** (`type: "shell"`) - Execute shell commands (disabled by default)
   - `command`: Executable path
   - `arguments`: Command arguments

3. **HTTP** (`type: "http"`) - Send HTTP requests
   - `url`: Request URL
   - `method`: HTTP method
   - `headers`: Optional headers
   - `bodyTemplate`: Optional body with placeholders (`{number}`, `{value}`, etc.)

## Platform Notes

- **Accessibility Permission Required**: CGEvent keyboard injection requires granting Accessibility permission in System Preferences > Security & Privacy > Privacy > Accessibility
- Uses `managed-midi` (Commons.Music.Midi) for MIDI I/O
- Can be installed as a LaunchAgent at `~/Library/LaunchAgents/`
- Config file permissions are set to 0600 (owner read/write only)
- Shell command executor is disabled by default for security
- Debug logging is enabled by default - all MIDI events and actions are logged

## Dependencies

- **managed-midi** (Commons.Music.Midi) - Cross-platform MIDI library
- **Microsoft.Extensions.Logging.Abstractions** - Logging interfaces
- **ASP.NET Core** - Web API hosting (included in .NET SDK)
