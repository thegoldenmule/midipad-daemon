namespace MidiPadDaemon.Core.Models;

public sealed record MidiConfig(
    string ActiveProfileId,
    List<MidiProfile> Profiles,
    bool VerboseLogging = false
)
{
    public static MidiConfig CreateDefault() => new(
        "default",
        [
            new MidiProfile("default", "Default Profile", [])
        ],
        VerboseLogging: false
    );
}
