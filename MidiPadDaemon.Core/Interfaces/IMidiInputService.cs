using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.Core.Interfaces;

public interface IMidiInputService : IAsyncDisposable
{
    event EventHandler<MidiInputEvent>? MidiEventReceived;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<MidiDeviceInfo> GetAvailableDevices();
}
