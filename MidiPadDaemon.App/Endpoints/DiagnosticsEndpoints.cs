using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.App.Endpoints;

public static class DiagnosticsEndpoints
{
    private static DateTimeOffset _startTime = DateTimeOffset.Now;
    private static MidiInputEvent? _lastEvent;
    private static int _eventCount;

    public static void MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/diagnostics").WithTags("Diagnostics");

        group.MapGet("/status", GetStatus)
            .WithName("GetStatus")
            .WithSummary("Get daemon status information");

        group.MapGet("/devices", GetDevices)
            .WithName("GetDevices")
            .WithSummary("List available MIDI devices");

        group.MapPost("/test-action/{bindingId}", TestAction)
            .WithName("TestAction")
            .WithSummary("Test execute a binding's actions");

        group.MapGet("/keys", GetAvailableKeys)
            .WithName("GetAvailableKeys")
            .WithSummary("List all available key names for keyboard actions");
    }

    public static void RecordEvent(MidiInputEvent ev)
    {
        _lastEvent = ev;
        Interlocked.Increment(ref _eventCount);
    }

    public static void SetStartTime(DateTimeOffset startTime)
    {
        _startTime = startTime;
    }

    private static IResult GetStatus(IConfigStore configStore, ILogger<Program> logger)
    {
        logger.LogDebug("GET /diagnostics/status");

        var config = configStore.Current;
        var uptime = DateTimeOffset.Now - _startTime;

        return Results.Ok(new
        {
            status = "running",
            startTime = _startTime,
            uptime = uptime.ToString(@"d\.hh\:mm\:ss"),
            uptimeSeconds = (int)uptime.TotalSeconds,
            activeProfile = config.ActiveProfileId,
            profileCount = config.Profiles.Count,
            bindingCount = config.Profiles
                .FirstOrDefault(p => p.Id == config.ActiveProfileId)?
                .Bindings.Count ?? 0,
            eventCount = _eventCount,
            lastEvent = _lastEvent is null ? null : new
            {
                deviceId = _lastEvent.DeviceId,
                channel = _lastEvent.Channel,
                eventType = _lastEvent.EventType.ToString(),
                number = _lastEvent.Number,
                value = _lastEvent.Value,
                timestamp = _lastEvent.Timestamp
            }
        });
    }

    private static IResult GetDevices(IMidiInputService midiService, ILogger<Program> logger)
    {
        logger.LogDebug("GET /diagnostics/devices");

        var devices = midiService.GetAvailableDevices();

        logger.LogInformation("Found {Count} MIDI devices", devices.Count);

        return Results.Ok(new
        {
            count = devices.Count,
            devices = devices.Select(d => new
            {
                d.Id,
                d.Name,
                d.Manufacturer,
                d.IsInput,
                d.IsOutput
            })
        });
    }

    private static async Task<IResult> TestAction(
        string bindingId,
        IConfigStore configStore,
        IMappingEngine mappingEngine,
        ILogger<Program> logger)
    {
        logger.LogInformation("POST /diagnostics/test-action/{BindingId}", bindingId);

        var config = await configStore.LoadAsync();
        var profile = config.Profiles.FirstOrDefault(p => p.Id == config.ActiveProfileId);

        var binding = profile?.Bindings.FirstOrDefault(b => b.Id == bindingId);
        if (binding is null)
        {
            logger.LogWarning("Binding not found: {BindingId}", bindingId);
            return Results.NotFound(new { error = $"Binding '{bindingId}' not found" });
        }

        // Create a fake MIDI event to trigger the binding
        var fakeEvent = new MidiInputEvent(
            DeviceId: "test",
            Channel: binding.Channel,
            EventType: binding.EventType,
            Number: binding.Number,
            Value: binding.ValueMatch ?? 127, // Default velocity if not specified
            Timestamp: DateTimeOffset.Now
        );

        logger.LogInformation(
            "Testing binding {BindingId} with fake event: Type={EventType}, Number={Number}",
            bindingId,
            binding.EventType,
            binding.Number);

        await mappingEngine.HandleEventAsync(fakeEvent);

        return Results.Ok(new
        {
            message = $"Triggered {binding.Actions.Count} action(s) for binding '{bindingId}'",
            binding = new
            {
                binding.Id,
                binding.EventType,
                binding.Channel,
                binding.Number,
                actionCount = binding.Actions.Count
            }
        });
    }

    private static IResult GetAvailableKeys(
        MidiPadDaemon.Interop.Interfaces.IKeyCodeMapper keyCodeMapper,
        ILogger<Program> logger)
    {
        logger.LogDebug("GET /diagnostics/keys");

        var mappings = keyCodeMapper.GetAllMappings();

        return Results.Ok(new
        {
            count = mappings.Count,
            keys = mappings
                .OrderBy(k => k.Key)
                .Select(k => new { name = k.Key, keyCode = $"0x{k.Value:X2}" })
        });
    }
}
