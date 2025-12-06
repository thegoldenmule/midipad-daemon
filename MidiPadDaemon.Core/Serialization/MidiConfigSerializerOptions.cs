using System.Text.Json;
using System.Text.Json.Serialization;

namespace MidiPadDaemon.Core.Serialization;

public static class MidiConfigSerializerOptions
{
    private static JsonSerializerOptions? _options;

    public static JsonSerializerOptions Default => _options ??= CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
        };

        // Add enum converters for string serialization
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
