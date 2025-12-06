using System.Text.Json.Serialization;

namespace MidiPadDaemon.Core.Models;

public enum KeyboardMode
{
    Press,      // Default: key down + key up
    ToggleHold  // First hit holds, second hit releases
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(KeyboardActionConfig), "keyboard")]
[JsonDerivedType(typeof(ShellCommandActionConfig), "shell")]
[JsonDerivedType(typeof(HttpRequestActionConfig), "http")]
public abstract record ActionConfig;

public sealed record KeyboardActionConfig(
    string Key,          // e.g. "Enter", "F", "F1", "Space"
    bool Command = false,
    bool Option = false,
    bool Control = false,
    bool Shift = false,
    KeyboardMode Mode = KeyboardMode.Press
) : ActionConfig;

public sealed record ShellCommandActionConfig(
    string Command,      // e.g. "/usr/bin/osascript"
    string Arguments = ""
) : ActionConfig;

public sealed record HttpRequestActionConfig(
    string Url,
    string Method = "POST",
    Dictionary<string, string>? Headers = null,
    string? BodyTemplate = null
) : ActionConfig;
