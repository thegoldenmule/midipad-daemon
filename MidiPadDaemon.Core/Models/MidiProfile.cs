namespace MidiPadDaemon.Core.Models;

public sealed record MidiProfile(
    string Id,
    string Name,
    List<MidiBinding> Bindings
);
