namespace MidiPadDaemon.Core.Models;

public sealed record MidiBinding(
    string Id,
    string DeviceId,      // or "any" for any device
    int Channel,
    MidiEventType EventType,
    int Number,           // note/CC number
    int? ValueMatch,      // optional; null = any value
    List<ActionConfig> Actions
);
