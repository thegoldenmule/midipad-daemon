namespace MidiPadDaemon.Core.Models;

public sealed record MidiInputEvent(
    string DeviceId,
    int Channel,
    MidiEventType EventType,
    int Number,      // note number or CC number
    int Value,       // velocity or CC value
    DateTimeOffset Timestamp
);
