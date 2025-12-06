using MidiPadDaemon.Interop.Interfaces;
using MidiPadDaemon.Interop.Native;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Interop;

/// <summary>
/// Injects keyboard events using macOS CGEvent API.
/// Requires Accessibility permission in System Preferences.
/// </summary>
public sealed class KeyboardInjector : IKeyboardInjector
{
    private readonly ILogger<KeyboardInjector> _logger;

    public KeyboardInjector(ILogger<KeyboardInjector> logger)
    {
        _logger = logger;
    }

    public void SendKeyPress(ushort keyCode, bool command = false, bool option = false, bool control = false, bool shift = false)
    {
        _logger.LogDebug(
            "SendKeyPress: keyCode=0x{KeyCode:X2}, command={Command}, option={Option}, control={Control}, shift={Shift}",
            keyCode, command, option, control, shift);

        SendKeyDown(keyCode, command, option, control, shift);
        SendKeyUp(keyCode, command, option, control, shift);
    }

    public void SendKeyDown(ushort keyCode, bool command = false, bool option = false, bool control = false, bool shift = false)
    {
        var flags = CoreGraphicsInterop.BuildFlags(command, option, control, shift);

        _logger.LogDebug("SendKeyDown: keyCode=0x{KeyCode:X2}, flags={Flags}", keyCode, flags);

        var eventRef = CoreGraphicsInterop.CGEventCreateKeyboardEvent(IntPtr.Zero, keyCode, keyDown: true);
        if (eventRef == IntPtr.Zero)
        {
            _logger.LogError("Failed to create key down event for keyCode=0x{KeyCode:X2}", keyCode);
            return;
        }

        try
        {
            if (flags != CGEventFlags.None)
            {
                CoreGraphicsInterop.CGEventSetFlags(eventRef, flags);
            }

            CoreGraphicsInterop.CGEventPost(CGEventTapLocation.HID, eventRef);
            _logger.LogDebug("Posted key down event for keyCode=0x{KeyCode:X2}", keyCode);
        }
        finally
        {
            CoreGraphicsInterop.CFRelease(eventRef);
        }
    }

    public void SendKeyUp(ushort keyCode, bool command = false, bool option = false, bool control = false, bool shift = false)
    {
        var flags = CoreGraphicsInterop.BuildFlags(command, option, control, shift);

        _logger.LogDebug("SendKeyUp: keyCode=0x{KeyCode:X2}, flags={Flags}", keyCode, flags);

        var eventRef = CoreGraphicsInterop.CGEventCreateKeyboardEvent(IntPtr.Zero, keyCode, keyDown: false);
        if (eventRef == IntPtr.Zero)
        {
            _logger.LogError("Failed to create key up event for keyCode=0x{KeyCode:X2}", keyCode);
            return;
        }

        try
        {
            if (flags != CGEventFlags.None)
            {
                CoreGraphicsInterop.CGEventSetFlags(eventRef, flags);
            }

            CoreGraphicsInterop.CGEventPost(CGEventTapLocation.HID, eventRef);
            _logger.LogDebug("Posted key up event for keyCode=0x{KeyCode:X2}", keyCode);
        }
        finally
        {
            CoreGraphicsInterop.CFRelease(eventRef);
        }
    }
}
