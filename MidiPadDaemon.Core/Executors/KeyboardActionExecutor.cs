using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;
using MidiPadDaemon.Interop.Interfaces;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Core.Executors;

/// <summary>
/// Executes keyboard actions by injecting key events via CGEvent.
/// </summary>
public sealed class KeyboardActionExecutor : IActionExecutor
{
    private readonly ILogger<KeyboardActionExecutor> _logger;
    private readonly IKeyCodeMapper _keyCodeMapper;
    private readonly IKeyboardInjector _keyboardInjector;

    public KeyboardActionExecutor(
        ILogger<KeyboardActionExecutor> logger,
        IKeyCodeMapper keyCodeMapper,
        IKeyboardInjector keyboardInjector)
    {
        _logger = logger;
        _keyCodeMapper = keyCodeMapper;
        _keyboardInjector = keyboardInjector;
    }

    public bool CanExecute(ActionConfig action) => action is KeyboardActionConfig;

    public Task ExecuteAsync(ActionConfig action, MidiInputEvent sourceEvent, CancellationToken ct = default)
    {
        if (action is not KeyboardActionConfig keyboard)
        {
            _logger.LogWarning("KeyboardActionExecutor received non-keyboard action");
            return Task.CompletedTask;
        }

        var keyCode = _keyCodeMapper.ToVirtualKeyCode(keyboard.Key);
        if (keyCode is null)
        {
            _logger.LogWarning("Unknown key name: {Key}", keyboard.Key);
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "Sending key press: Key={Key} (0x{KeyCode:X2}), Cmd={Command}, Opt={Option}, Ctrl={Control}, Shift={Shift}",
            keyboard.Key,
            keyCode.Value,
            keyboard.Command,
            keyboard.Option,
            keyboard.Control,
            keyboard.Shift);

        _keyboardInjector.SendKeyPress(
            keyCode.Value,
            keyboard.Command,
            keyboard.Option,
            keyboard.Control,
            keyboard.Shift);

        _logger.LogInformation(
            "Key press sent: {Key} (triggered by MIDI note {Note})",
            keyboard.Key,
            sourceEvent.Number);

        return Task.CompletedTask;
    }
}
