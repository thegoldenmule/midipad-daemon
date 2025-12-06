using System.Runtime.InteropServices;

namespace MidiPadDaemon.Interop.Native;

internal static class CoreGraphicsInterop
{
    private const string CoreGraphicsLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreGraphicsLib)]
    internal static extern IntPtr CGEventCreateKeyboardEvent(
        IntPtr source,
        ushort virtualKey,
        [MarshalAs(UnmanagedType.I1)] bool keyDown);

    [DllImport(CoreGraphicsLib)]
    internal static extern void CGEventPost(
        CGEventTapLocation tap,
        IntPtr eventRef);

    [DllImport(CoreGraphicsLib)]
    internal static extern void CGEventSetFlags(
        IntPtr eventRef,
        CGEventFlags flags);

    [DllImport(CoreGraphicsLib)]
    internal static extern CGEventFlags CGEventGetFlags(IntPtr eventRef);

    [DllImport(CoreFoundationLib)]
    internal static extern void CFRelease(IntPtr cfTypeRef);

    /// <summary>
    /// Builds CGEventFlags from modifier booleans.
    /// </summary>
    internal static CGEventFlags BuildFlags(bool command, bool option, bool control, bool shift)
    {
        var flags = CGEventFlags.None;
        if (command) flags |= CGEventFlags.MaskCommand;
        if (option) flags |= CGEventFlags.MaskAlternate;
        if (control) flags |= CGEventFlags.MaskControl;
        if (shift) flags |= CGEventFlags.MaskShift;
        return flags;
    }
}
