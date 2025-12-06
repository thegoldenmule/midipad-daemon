using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.Core.Interfaces;

public interface IMappingEngine
{
    void UpdateConfig(MidiConfig config);
    Task HandleEventAsync(MidiInputEvent midiEvent, CancellationToken ct = default);
}
