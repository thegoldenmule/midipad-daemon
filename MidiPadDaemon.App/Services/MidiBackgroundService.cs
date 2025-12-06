using MidiPadDaemon.App.Endpoints;
using MidiPadDaemon.Core.Interfaces;

namespace MidiPadDaemon.App.Services;

/// <summary>
/// Background service that starts MIDI input and wires events to the mapping engine.
/// </summary>
public sealed class MidiBackgroundService : IHostedService
{
    private readonly ILogger<MidiBackgroundService> _logger;
    private readonly IMidiInputService _midiInputService;
    private readonly IMappingEngine _mappingEngine;
    private readonly IConfigStore _configStore;

    public MidiBackgroundService(
        ILogger<MidiBackgroundService> logger,
        IMidiInputService midiInputService,
        IMappingEngine mappingEngine,
        IConfigStore configStore)
    {
        _logger = logger;
        _midiInputService = midiInputService;
        _mappingEngine = mappingEngine;
        _configStore = configStore;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MidiBackgroundService starting...");

        // Record start time for diagnostics
        DiagnosticsEndpoints.SetStartTime(DateTimeOffset.Now);

        // Load initial configuration
        var config = await _configStore.LoadAsync(cancellationToken);
        _mappingEngine.UpdateConfig(config);

        _logger.LogInformation(
            "Loaded configuration - Active profile: {ActiveProfile}, Bindings: {BindingCount}",
            config.ActiveProfileId,
            config.Profiles.FirstOrDefault(p => p.Id == config.ActiveProfileId)?.Bindings.Count ?? 0);

        // Wire up MIDI events to mapping engine
        _midiInputService.MidiEventReceived += OnMidiEventReceived;

        // Start listening for MIDI events
        await _midiInputService.StartAsync(cancellationToken);

        _logger.LogInformation("MidiBackgroundService started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MidiBackgroundService stopping...");

        _midiInputService.MidiEventReceived -= OnMidiEventReceived;
        await _midiInputService.StopAsync(cancellationToken);

        _logger.LogInformation("MidiBackgroundService stopped");
    }

    private async void OnMidiEventReceived(object? sender, Core.Models.MidiInputEvent e)
    {
        try
        {
            // Record for diagnostics
            DiagnosticsEndpoints.RecordEvent(e);

            // Process through mapping engine
            await _mappingEngine.HandleEventAsync(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MIDI event");
        }
    }
}
