using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.Core.Interfaces;

public interface IConfigStore
{
    MidiConfig Current { get; }
    Task<MidiConfig> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(MidiConfig config, CancellationToken ct = default);
    event EventHandler<MidiConfig>? ConfigChanged;
}
