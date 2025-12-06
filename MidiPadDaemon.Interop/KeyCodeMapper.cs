using MidiPadDaemon.Interop.Interfaces;

namespace MidiPadDaemon.Interop;

/// <summary>
/// Maps key names to macOS virtual key codes (HIToolbox kVK_* constants).
/// </summary>
public sealed class KeyCodeMapper : IKeyCodeMapper
{
    // macOS virtual key codes from HIToolbox/Events.h
    private static readonly Dictionary<string, ushort> KeyNameToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // Letters
        ["A"] = 0x00,
        ["S"] = 0x01,
        ["D"] = 0x02,
        ["F"] = 0x03,
        ["H"] = 0x04,
        ["G"] = 0x05,
        ["Z"] = 0x06,
        ["X"] = 0x07,
        ["C"] = 0x08,
        ["V"] = 0x09,
        ["B"] = 0x0B,
        ["Q"] = 0x0C,
        ["W"] = 0x0D,
        ["E"] = 0x0E,
        ["R"] = 0x0F,
        ["Y"] = 0x10,
        ["T"] = 0x11,
        ["O"] = 0x1F,
        ["U"] = 0x20,
        ["I"] = 0x22,
        ["P"] = 0x23,
        ["L"] = 0x25,
        ["J"] = 0x26,
        ["K"] = 0x28,
        ["N"] = 0x2D,
        ["M"] = 0x2E,

        // Numbers (top row)
        ["1"] = 0x12,
        ["2"] = 0x13,
        ["3"] = 0x14,
        ["4"] = 0x15,
        ["5"] = 0x17,
        ["6"] = 0x16,
        ["7"] = 0x1A,
        ["8"] = 0x1C,
        ["9"] = 0x19,
        ["0"] = 0x1D,

        // Function keys
        ["F1"] = 0x7A,
        ["F2"] = 0x78,
        ["F3"] = 0x63,
        ["F4"] = 0x76,
        ["F5"] = 0x60,
        ["F6"] = 0x61,
        ["F7"] = 0x62,
        ["F8"] = 0x64,
        ["F9"] = 0x65,
        ["F10"] = 0x6D,
        ["F11"] = 0x67,
        ["F12"] = 0x6F,
        ["F13"] = 0x69,
        ["F14"] = 0x6B,
        ["F15"] = 0x71,
        ["F16"] = 0x6A,
        ["F17"] = 0x40,
        ["F18"] = 0x4F,
        ["F19"] = 0x50,
        ["F20"] = 0x5A,

        // Special keys
        ["Return"] = 0x24,
        ["Enter"] = 0x24,
        ["Tab"] = 0x30,
        ["Space"] = 0x31,
        ["Backspace"] = 0x33,
        ["Delete"] = 0x33,
        ["Escape"] = 0x35,
        ["Esc"] = 0x35,
        ["Command"] = 0x37,
        ["Cmd"] = 0x37,
        ["Shift"] = 0x38,
        ["CapsLock"] = 0x39,
        ["Option"] = 0x3A,
        ["Alt"] = 0x3A,
        ["Control"] = 0x3B,
        ["Ctrl"] = 0x3B,
        ["RightShift"] = 0x3C,
        ["RightOption"] = 0x3D,
        ["RightControl"] = 0x3E,
        ["Function"] = 0x3F,
        ["Fn"] = 0x3F,

        // Arrow keys
        ["Left"] = 0x7B,
        ["Right"] = 0x7C,
        ["Down"] = 0x7D,
        ["Up"] = 0x7E,
        ["LeftArrow"] = 0x7B,
        ["RightArrow"] = 0x7C,
        ["DownArrow"] = 0x7D,
        ["UpArrow"] = 0x7E,

        // Navigation
        ["Home"] = 0x73,
        ["End"] = 0x77,
        ["PageUp"] = 0x74,
        ["PageDown"] = 0x79,
        ["ForwardDelete"] = 0x75,

        // Punctuation and symbols
        ["Minus"] = 0x1B,
        ["-"] = 0x1B,
        ["Equal"] = 0x18,
        ["="] = 0x18,
        ["LeftBracket"] = 0x21,
        ["["] = 0x21,
        ["RightBracket"] = 0x1E,
        ["]"] = 0x1E,
        ["Semicolon"] = 0x29,
        [";"] = 0x29,
        ["Quote"] = 0x27,
        ["'"] = 0x27,
        ["Backslash"] = 0x2A,
        ["\\"] = 0x2A,
        ["Comma"] = 0x2B,
        [","] = 0x2B,
        ["Period"] = 0x2F,
        ["."] = 0x2F,
        ["Slash"] = 0x2C,
        ["/"] = 0x2C,
        ["Grave"] = 0x32,
        ["`"] = 0x32,

        // Numpad
        ["Numpad0"] = 0x52,
        ["Numpad1"] = 0x53,
        ["Numpad2"] = 0x54,
        ["Numpad3"] = 0x55,
        ["Numpad4"] = 0x56,
        ["Numpad5"] = 0x57,
        ["Numpad6"] = 0x58,
        ["Numpad7"] = 0x59,
        ["Numpad8"] = 0x5B,
        ["Numpad9"] = 0x5C,
        ["NumpadDecimal"] = 0x41,
        ["NumpadMultiply"] = 0x43,
        ["NumpadPlus"] = 0x45,
        ["NumpadClear"] = 0x47,
        ["NumpadDivide"] = 0x4B,
        ["NumpadEnter"] = 0x4C,
        ["NumpadMinus"] = 0x4E,
        ["NumpadEquals"] = 0x51,

        // Media keys (may require special handling)
        ["VolumeUp"] = 0x48,
        ["VolumeDown"] = 0x49,
        ["Mute"] = 0x4A,
    };

    private static readonly Dictionary<ushort, string> CodeToKeyName;

    static KeyCodeMapper()
    {
        // Build reverse lookup (prefer shorter/primary names)
        CodeToKeyName = new Dictionary<ushort, string>();
        foreach (var kvp in KeyNameToCode)
        {
            if (!CodeToKeyName.ContainsKey(kvp.Value))
            {
                CodeToKeyName[kvp.Value] = kvp.Key;
            }
        }
    }

    public ushort? ToVirtualKeyCode(string keyName)
    {
        return KeyNameToCode.TryGetValue(keyName, out var code) ? code : null;
    }

    public string? ToKeyName(ushort virtualKeyCode)
    {
        return CodeToKeyName.TryGetValue(virtualKeyCode, out var name) ? name : null;
    }

    public IReadOnlyDictionary<string, ushort> GetAllMappings() => KeyNameToCode;
}
