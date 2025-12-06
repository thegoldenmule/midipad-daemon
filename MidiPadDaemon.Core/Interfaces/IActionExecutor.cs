using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.Core.Interfaces;

public interface IActionExecutor
{
    bool CanExecute(ActionConfig action);
    Task ExecuteAsync(ActionConfig action, MidiInputEvent sourceEvent, CancellationToken ct = default);
}
