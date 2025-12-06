namespace MidiPadDaemon.Interop.Interfaces;

public interface IKeyboardInjector
{
    void SendKeyPress(ushort keyCode, bool command = false, bool option = false, bool control = false, bool shift = false);
    void SendKeyDown(ushort keyCode, bool command = false, bool option = false, bool control = false, bool shift = false);
    void SendKeyUp(ushort keyCode, bool command = false, bool option = false, bool control = false, bool shift = false);
}
