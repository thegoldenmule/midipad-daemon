namespace MidiPadDaemon.Core.Models;

public sealed record MidiDeviceInfo(
    string Id,
    string Name,
    string? Manufacturer,
    bool IsInput,
    bool IsOutput
);
