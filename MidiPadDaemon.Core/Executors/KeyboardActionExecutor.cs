using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, bool> _heldKeys = new();

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

        if (keyboard.Mode == KeyboardMode.ToggleHold)
        {
            ExecuteToggleHold(keyboard, keyCode.Value, sourceEvent);
        }
        else
        {
            ExecutePress(keyboard, keyCode.Value, sourceEvent);
        }

        return Task.CompletedTask;
    }

    private void ExecutePress(KeyboardActionConfig keyboard, ushort keyCode, MidiInputEvent sourceEvent)
    {
        var keyCombo = FormatKeyCombo(keyboard);

        _logger.LogDebug(
            "Sending key press: {KeyCombo} (0x{KeyCode:X2})",
            keyCombo,
            keyCode);

        _keyboardInjector.SendKeyPress(
            keyCode,
            keyboard.Command,
            keyboard.Option,
            keyboard.Control,
            keyboard.Shift);

        _logger.LogInformation(
            "Key press sent: {KeyCombo} (triggered by MIDI note {Note})",
            keyCombo,
            sourceEvent.Number);
    }

    private void ExecuteToggleHold(KeyboardActionConfig keyboard, ushort keyCode, MidiInputEvent sourceEvent)
    {
        var stateKey = GetStateKey(keyboard);
        var keyCombo = FormatKeyCombo(keyboard);
        var isHeld = _heldKeys.GetValueOrDefault(stateKey, false);

        if (isHeld)
        {
            _keyboardInjector.SendKeyUp(
                keyCode,
                keyboard.Command,
                keyboard.Option,
                keyboard.Control,
                keyboard.Shift);

            _heldKeys[stateKey] = false;

            _logger.LogInformation(
                "Key released (toggle): {KeyCombo} (triggered by MIDI note {Note})",
                keyCombo,
                sourceEvent.Number);
        }
        else
        {
            _keyboardInjector.SendKeyDown(
                keyCode,
                keyboard.Command,
                keyboard.Option,
                keyboard.Control,
                keyboard.Shift);

            _heldKeys[stateKey] = true;

            _logger.LogInformation(
                "Key held (toggle): {KeyCombo} (triggered by MIDI note {Note})",
                keyCombo,
                sourceEvent.Number);
        }
    }

    private static string FormatKeyCombo(KeyboardActionConfig config)
    {
        var parts = new List<string>();
        if (config.Control) parts.Add("Ctrl");
        if (config.Option) parts.Add("Opt");
        if (config.Shift) parts.Add("Shift");
        if (config.Command) parts.Add("Cmd");
        parts.Add(config.Key);
        return string.Join("+", parts);
    }

    private static string GetStateKey(KeyboardActionConfig config)
        => $"{config.Key}:{config.Command}:{config.Option}:{config.Control}:{config.Shift}";
}
