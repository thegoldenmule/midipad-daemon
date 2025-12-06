namespace MidiPadDaemon.Interop.Native;

[Flags]
internal enum CGEventFlags : ulong
{
    None = 0,
    MaskAlphaShift = 1UL << 16,    // Caps Lock
    MaskShift = 1UL << 17,
    MaskControl = 1UL << 18,
    MaskAlternate = 1UL << 19,     // Option/Alt
    MaskCommand = 1UL << 20,
    MaskNumericPad = 1UL << 21,
    MaskHelp = 1UL << 22,
    MaskSecondaryFn = 1UL << 23,
}
