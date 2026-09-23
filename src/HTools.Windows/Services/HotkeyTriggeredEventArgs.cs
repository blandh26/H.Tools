namespace HTools.Windows.Services;

public sealed class HotkeyTriggeredEventArgs(int id, object? tag) : EventArgs
{
    public int Id { get; } = id;

    public object? Tag { get; } = tag;
}
