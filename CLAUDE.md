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

# Run the web host + MIDI services
dotnet run --project MidiPadDaemon.App/MidiPadDaemon.App.csproj

# Publish for Apple Silicon
dotnet publish MidiPadDaemon.App/MidiPadDaemon.App.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true

# Publish for Intel
dotnet publish MidiPadDaemon.App/MidiPadDaemon.App.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true
```

## Architecture

### Project Structure

- **MidiPadDaemon.App** - ASP.NET Core minimal API host + background services (entry point)
- **MidiPadDaemon.Core** - Mapping engine, config models, MIDI abstractions, services
- **MidiPadDaemon.Interop** - P/Invoke for macOS CGEvent keyboard injection

### Data Flow

```
Alesis Pad Hit → managed-midi → MidiInputService → MappingEngine → ActionExecutors
                                                                    ├─ KeyboardActionExecutor (CGEvent)
                                                                    ├─ ShellActionExecutor
                                                                    └─ HttpActionExecutor
```

### Key Components

- **MidiInputService** - Wraps managed-midi library, discovers MIDI devices, emits `MidiInputEvent`
- **MappingEngine** - Thread-safe lookup from MIDI events to bindings, dispatches to executors
- **ActionExecutors** - Implementations for keyboard (CGEvent P/Invoke), shell commands, HTTP requests
- **ConfigStore** - JSON persistence at `~/.midi-pad-daemon/config.json`, hot-reloadable

### Web API (Kestrel)

Bound to localhost only (e.g., `http://127.0.0.1:5005`):
- `GET /config` - Current configuration
- `PUT /config` - Replace configuration
- `PATCH /bindings/{id}` - Update single binding
- `POST /test-actions/{bindingId}` - Trigger action for debugging

## Platform Notes

- Requires macOS Accessibility permission for CGEvent keyboard injection
- Uses `managed-midi` (Commons.Music.Midi) for MIDI I/O
- Can be installed as a LaunchAgent at `~/Library/LaunchAgents/`
- Config file permissions should be restrictive (0600)
