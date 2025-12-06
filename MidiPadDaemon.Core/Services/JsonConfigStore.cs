using System.Text.Json;
using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;
using MidiPadDaemon.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Core.Services;

/// <summary>
/// Persists configuration to ~/.midi-pad-daemon/config.json.
/// Thread-safe for concurrent access.
/// </summary>
public sealed class JsonConfigStore : IConfigStore
{
    private readonly ILogger<JsonConfigStore> _logger;
    private readonly string _configPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private MidiConfig _current;

    public event EventHandler<MidiConfig>? ConfigChanged;

    public MidiConfig Current => _current;

    public JsonConfigStore(ILogger<JsonConfigStore> logger)
    {
        _logger = logger;

        // Config stored in ~/.midi-pad-daemon/config.json
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".midi-pad-daemon");
        _configPath = Path.Combine(configDir, "config.json");

        _logger.LogInformation("Config path: {ConfigPath}", _configPath);

        // Initialize with default config (will be overwritten by LoadAsync)
        _current = MidiConfig.CreateDefault();
    }

    public async Task<MidiConfig> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Loading config from {ConfigPath}", _configPath);

            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Config file not found, creating default config");
                await CreateDefaultConfigAsync(ct);
            }

            var json = await File.ReadAllTextAsync(_configPath, ct);
            var config = JsonSerializer.Deserialize<MidiConfig>(json, MidiConfigSerializerOptions.Default);

            if (config is null)
            {
                _logger.LogWarning("Failed to deserialize config, using default");
                config = MidiConfig.CreateDefault();
            }

            _current = config;
            _logger.LogInformation(
                "Loaded config with {ProfileCount} profiles, active profile: {ActiveProfile}",
                config.Profiles.Count,
                config.ActiveProfileId);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading config from {ConfigPath}", _configPath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(MidiConfig config, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Saving config to {ConfigPath}", _configPath);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(_configPath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                _logger.LogDebug("Created config directory: {Directory}", dir);
            }

            var json = JsonSerializer.Serialize(config, MidiConfigSerializerOptions.Default);
            await File.WriteAllTextAsync(_configPath, json, ct);

            // Set file permissions to owner-only on Unix
            SetFilePermissions(_configPath);

            _current = config;
            _logger.LogInformation(
                "Saved config with {ProfileCount} profiles, active profile: {ActiveProfile}",
                config.Profiles.Count,
                config.ActiveProfileId);

            // Notify listeners
            ConfigChanged?.Invoke(this, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving config to {ConfigPath}", _configPath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task CreateDefaultConfigAsync(CancellationToken ct)
    {
        var defaultConfig = MidiConfig.CreateDefault();

        // Ensure directory exists
        var dir = Path.GetDirectoryName(_configPath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            _logger.LogDebug("Created config directory: {Directory}", dir);
        }

        var json = JsonSerializer.Serialize(defaultConfig, MidiConfigSerializerOptions.Default);
        await File.WriteAllTextAsync(_configPath, json, ct);

        // Set file permissions to owner-only on Unix
        SetFilePermissions(_configPath);

        _logger.LogInformation("Created default config file at {ConfigPath}", _configPath);
    }

    private void SetFilePermissions(string path)
    {
        // On Unix systems, set file to owner read/write only (0600)
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                _logger.LogDebug("Set file permissions to 0600 for {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set file permissions for {Path}", path);
            }
        }
    }
}
