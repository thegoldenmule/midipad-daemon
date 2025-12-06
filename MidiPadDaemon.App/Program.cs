using System.Text.Json;
using System.Text.Json.Serialization;
using MidiPadDaemon.App.Endpoints;
using MidiPadDaemon.App.Services;
using MidiPadDaemon.Core.Executors;
using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Services;
using MidiPadDaemon.Interop;
using MidiPadDaemon.Interop.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization for API (enums as strings, camelCase)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Enable debug logging

builder.Services.AddLogging(logging =>
{
    logging.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "[HH:mm:ss] ";
        options.SingleLine = true;
    });
});

// Configure Kestrel to bind only to localhost (security)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5005);
});

// Register Interop services
builder.Services.AddSingleton<IKeyCodeMapper, KeyCodeMapper>();
builder.Services.AddSingleton<IKeyboardInjector, KeyboardInjector>();

// Register Core services
builder.Services.AddSingleton<IConfigStore, JsonConfigStore>();
builder.Services.AddSingleton<IMidiInputService, MidiInputService>();
builder.Services.AddSingleton<IMappingEngine, MappingEngine>();

// Register Action Executors
builder.Services.AddSingleton<IActionExecutor, KeyboardActionExecutor>();
builder.Services.AddSingleton<IActionExecutor>(sp =>
    new ShellCommandActionExecutor(
        sp.GetRequiredService<ILogger<ShellCommandActionExecutor>>(),
        enabled: false)); // Disabled by default for security
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IActionExecutor>(sp =>
    new HttpActionExecutor(
        sp.GetRequiredService<ILogger<HttpActionExecutor>>(),
        sp.GetRequiredService<IHttpClientFactory>().CreateClient()));

// Register Background Service
builder.Services.AddHostedService<MidiBackgroundService>();

var app = builder.Build();

// Log startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MidiPadDaemon starting...");
logger.LogInformation("Listening on http://localhost:5005");

// Map API endpoints
app.MapConfigEndpoints();
app.MapBindingsEndpoints();
app.MapDiagnosticsEndpoints();

// Root endpoint
app.MapGet("/", () => new
{
    name = "MidiPadDaemon",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET  /config              - Get current configuration",
        "PUT  /config              - Update configuration",
        "GET  /config/profiles     - List all profiles",
        "PUT  /config/profiles/active?profileId=xxx - Set active profile",
        "GET  /bindings            - List bindings in active profile",
        "GET  /bindings/{id}       - Get a specific binding",
        "POST /bindings            - Create a new binding",
        "PUT  /bindings/{id}       - Update a binding",
        "DELETE /bindings/{id}     - Delete a binding",
        "GET  /diagnostics/status  - Get daemon status",
        "GET  /diagnostics/devices - List MIDI devices",
        "POST /diagnostics/test-action/{bindingId} - Test a binding",
        "GET  /diagnostics/keys    - List available key names",
        "GET  /diagnostics/verbose - Get verbose logging state",
        "PUT  /diagnostics/verbose - Set verbose logging on or off"
    }
});

logger.LogInformation("MidiPadDaemon ready");

app.Run();
