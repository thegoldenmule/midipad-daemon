using Commons.Music.Midi;
using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Core.Services;

/// <summary>
/// Handles MIDI input device discovery and event capture using managed-midi.
/// </summary>
public sealed class MidiInputService : IMidiInputService
{
    private readonly ILogger<MidiInputService> _logger;
#pragma warning disable CS0618 // Type or member is obsolete - IMidiAccess2 will become identical
    private readonly IMidiAccess _midiAccess;
#pragma warning restore CS0618
    private readonly List<IMidiInput> _openInputs = new();
    private readonly object _lock = new();
    private bool _isRunning;

    public event EventHandler<MidiInputEvent>? MidiEventReceived;

    public MidiInputService(ILogger<MidiInputService> logger)
    {
        _logger = logger;
        _midiAccess = MidiAccessManager.Default;

        _logger.LogInformation("MidiInputService initialized with access type: {AccessType}", _midiAccess.GetType().Name);
    }

    public IReadOnlyList<MidiDeviceInfo> GetAvailableDevices()
    {
        var devices = new List<MidiDeviceInfo>();

        _logger.LogDebug("Scanning for MIDI devices...");

        foreach (var input in _midiAccess.Inputs)
        {
            var deviceInfo = new MidiDeviceInfo(
                Id: input.Id,
                Name: input.Name,
                Manufacturer: input.Manufacturer,
                IsInput: true,
                IsOutput: false
            );
            devices.Add(deviceInfo);
            _logger.LogDebug("Found input device: {Name} (ID: {Id})", input.Name, input.Id);
        }

        foreach (var output in _midiAccess.Outputs)
        {
            var deviceInfo = new MidiDeviceInfo(
                Id: output.Id,
                Name: output.Name,
                Manufacturer: output.Manufacturer,
                IsInput: false,
                IsOutput: true
            );
            devices.Add(deviceInfo);
            _logger.LogDebug("Found output device: {Name} (ID: {Id})", output.Name, output.Id);
        }

        _logger.LogInformation("Found {Count} MIDI devices", devices.Count);
        return devices;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("MidiInputService is already running");
                return;
            }
            _isRunning = true;
        }

        _logger.LogInformation("Starting MIDI input service...");

        // Find and open all available input devices
        // Filter for Alesis/Strike devices if found, otherwise open all
        var inputs = _midiAccess.Inputs.ToList();

        if (inputs.Count == 0)
        {
            _logger.LogWarning("No MIDI input devices found");
            return;
        }

        // Prefer Alesis/Strike devices
        var preferredInputs = inputs.Where(i =>
            i.Name.Contains("Alesis", StringComparison.OrdinalIgnoreCase) ||
            i.Name.Contains("Strike", StringComparison.OrdinalIgnoreCase) ||
            i.Name.Contains("Multipad", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        var devicesToOpen = preferredInputs.Count > 0 ? preferredInputs : inputs;

        foreach (var inputPort in devicesToOpen)
        {
            try
            {
                _logger.LogInformation("Opening MIDI input: {Name} (ID: {Id})", inputPort.Name, inputPort.Id);

                var input = await _midiAccess.OpenInputAsync(inputPort.Id);
                input.MessageReceived += OnMidiMessageReceived;

                lock (_lock)
                {
                    _openInputs.Add(input);
                }

                _logger.LogInformation("Successfully opened MIDI input: {Name}", inputPort.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open MIDI input: {Name}", inputPort.Name);
            }
        }

        _logger.LogInformation("MIDI input service started with {Count} open inputs", _openInputs.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping MIDI input service...");

        List<IMidiInput> inputsToClose;
        lock (_lock)
        {
            _isRunning = false;
            inputsToClose = new List<IMidiInput>(_openInputs);
            _openInputs.Clear();
        }

        foreach (var input in inputsToClose)
        {
            try
            {
                input.MessageReceived -= OnMidiMessageReceived;
                await input.CloseAsync();
                _logger.LogDebug("Closed MIDI input");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing MIDI input");
            }
        }

        _logger.LogInformation("MIDI input service stopped");
    }

    private void OnMidiMessageReceived(object? sender, MidiReceivedEventArgs e)
    {
        try
        {
            var data = e.Data;
            if (data == null || data.Length == 0)
            {
                return;
            }

            // Parse the MIDI message
            var statusByte = data[0];
            var messageType = statusByte & 0xF0;
            var channel = (statusByte & 0x0F) + 1; // MIDI channels are 1-16

            MidiEventType? eventType = messageType switch
            {
                0x90 => MidiEventType.NoteOn,
                0x80 => MidiEventType.NoteOff,
                0xB0 => MidiEventType.ControlChange,
                0xC0 => MidiEventType.ProgramChange,
                _ => null
            };

            if (eventType == null)
            {
                _logger.LogDebug("Ignoring MIDI message type: 0x{MessageType:X2}", messageType);
                return;
            }

            int number = data.Length > 1 ? data[1] : 0;
            int value = data.Length > 2 ? data[2] : 0;

            // Note: NoteOn with velocity 0 is treated as NoteOff
            if (eventType == MidiEventType.NoteOn && value == 0)
            {
                eventType = MidiEventType.NoteOff;
            }

            // Get device ID from sender if possible
            var deviceId = (sender as IMidiInput)?.Details?.Id ?? "unknown";

            var midiEvent = new MidiInputEvent(
                DeviceId: deviceId,
                Channel: channel,
                EventType: eventType.Value,
                Number: number,
                Value: value,
                Timestamp: DateTimeOffset.Now
            );

            _logger.LogDebug(
                "MIDI Event: Device={DeviceId}, Channel={Channel}, Type={EventType}, Number={Number}, Value={Value}",
                midiEvent.DeviceId,
                midiEvent.Channel,
                midiEvent.EventType,
                midiEvent.Number,
                midiEvent.Value);

            MidiEventReceived?.Invoke(this, midiEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MIDI message");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
