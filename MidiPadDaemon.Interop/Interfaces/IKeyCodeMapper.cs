namespace MidiPadDaemon.Interop.Interfaces;

public interface IKeyCodeMapper
{
    ushort? ToVirtualKeyCode(string keyName);
    string? ToKeyName(ushort virtualKeyCode);
    IReadOnlyDictionary<string, ushort> GetAllMappings();
}
