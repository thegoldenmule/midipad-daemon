using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Core.Services;

/// <summary>
/// Maps MIDI events to actions through the active profile's bindings.
/// Thread-safe for concurrent updates and event handling.
/// </summary>
public sealed class MappingEngine : IMappingEngine
{
    private readonly ILogger<MappingEngine> _logger;
    private readonly IReadOnlyList<IActionExecutor> _executors;
    private MidiConfig _config;

    public MappingEngine(
        ILogger<MappingEngine> logger,
        IEnumerable<IActionExecutor> executors,
        IConfigStore configStore)
    {
        _logger = logger;
        _executors = executors.ToList();
        _config = configStore.Current;

        // Subscribe to config changes
        configStore.ConfigChanged += (_, newConfig) => UpdateConfig(newConfig);

        _logger.LogInformation(
            "MappingEngine initialized with {ExecutorCount} executors",
            _executors.Count);

        foreach (var executor in _executors)
        {
            _logger.LogDebug("Registered executor: {ExecutorType}", executor.GetType().Name);
        }
    }

    public void UpdateConfig(MidiConfig config)
    {
        Interlocked.Exchange(ref _config, config);

        _logger.LogInformation(
            "Config updated - Active profile: {ActiveProfile}, Bindings count: {BindingCount}",
            config.ActiveProfileId,
            GetActiveProfile(config)?.Bindings.Count ?? 0);
    }

    public async Task HandleEventAsync(MidiInputEvent midiEvent, CancellationToken ct = default)
    {
        var config = _config; // Snapshot for thread safety
        var verbose = config.VerboseLogging;

        var bindings = FindMatchingBindings(midiEvent, config).ToList();

        if (bindings.Count == 0)
        {
            // Only log unmatched events when verbose logging is enabled
            if (verbose)
            {
                _logger.LogInformation(
                    "MIDI event (no binding): Type={EventType}, Channel={Channel}, Number={Number}, Value={Value}",
                    midiEvent.EventType,
                    midiEvent.Channel,
                    midiEvent.Number,
                    midiEvent.Value);
            }
            return;
        }

        // Always log matched events
        _logger.LogInformation(
            "MIDI event matched {BindingCount} binding(s): Type={EventType}, Channel={Channel}, Number={Number}, Value={Value}",
            bindings.Count,
            midiEvent.EventType,
            midiEvent.Channel,
            midiEvent.Number,
            midiEvent.Value);

        foreach (var binding in bindings)
        {
            _logger.LogDebug("Processing binding: {BindingId} with {ActionCount} action(s)",
                binding.Id,
                binding.Actions.Count);

            foreach (var action in binding.Actions)
            {
                await ExecuteActionAsync(action, midiEvent, ct);
            }
        }
    }

    private async Task ExecuteActionAsync(ActionConfig action, MidiInputEvent midiEvent, CancellationToken ct)
    {
        var executor = _executors.FirstOrDefault(e => e.CanExecute(action));

        if (executor is null)
        {
            _logger.LogWarning(
                "No executor found for action type: {ActionType}",
                action.GetType().Name);
            return;
        }

        _logger.LogDebug(
            "Executing action {ActionType} with executor {ExecutorType}",
            action.GetType().Name,
            executor.GetType().Name);

        try
        {
            await executor.ExecuteAsync(action, midiEvent, ct);
            _logger.LogDebug("Action executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error executing action {ActionType}",
                action.GetType().Name);
        }
    }

    private IEnumerable<MidiBinding> FindMatchingBindings(MidiInputEvent ev, MidiConfig config)
    {
        var profile = GetActiveProfile(config);
        if (profile is null)
        {
            _logger.LogWarning("No active profile found: {ActiveProfileId}", config.ActiveProfileId);
            yield break;
        }

        foreach (var binding in profile.Bindings)
        {
            if (IsMatch(binding, ev))
            {
                yield return binding;
            }
        }
    }

    private static bool IsMatch(MidiBinding binding, MidiInputEvent ev)
    {
        // Device must match or be "any"
        if (binding.DeviceId != "any" &&
            !string.Equals(binding.DeviceId, ev.DeviceId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Channel must match
        if (binding.Channel != ev.Channel)
        {
            return false;
        }

        // Event type must match
        if (binding.EventType != ev.EventType)
        {
            return false;
        }

        // Number (note/CC) must match
        if (binding.Number != ev.Number)
        {
            return false;
        }

        // Value must match if specified
        if (binding.ValueMatch.HasValue && binding.ValueMatch.Value != ev.Value)
        {
            return false;
        }

        return true;
    }

    private static MidiProfile? GetActiveProfile(MidiConfig config)
    {
        return config.Profiles.FirstOrDefault(p =>
            string.Equals(p.Id, config.ActiveProfileId, StringComparison.OrdinalIgnoreCase));
    }
}
