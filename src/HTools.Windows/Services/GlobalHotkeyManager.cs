using HTools.Windows.Interop;

namespace HTools.Windows.Services;

/// <summary>Registers/unregisters arbitrary global Ctrl/Alt/Shift/Win+key combos and reports which
/// caller-supplied <c>tag</c> fired. Callers own the mapping from tag to meaning (e.g. "paste slot 3");
/// this class only owns the Win32 registration lifecycle.</summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private const int HotkeyIdBase = 0xC000;

    private readonly NativeMessageLoop _loop;
    private readonly Dictionary<int, object?> _tagsById = new();
    private int _nextId = HotkeyIdBase;

    public GlobalHotkeyManager(NativeMessageLoop loop)
    {
        _loop = loop;
        _loop.HotkeyPressed += OnHotkeyPressed;
    }

    public event EventHandler<HotkeyTriggeredEventArgs>? HotkeyTriggered;

    /// <summary>Registers a combo and returns its id, or null if it's already claimed by another app.</summary>
    public int? Register(HotkeyModifiers modifiers, uint virtualKey, object? tag = null)
    {
        var id = _nextId++;
        if (!_loop.RegisterHotkey(id, (uint)modifiers, virtualKey))
        {
            return null;
        }

        _tagsById[id] = tag;
        return id;
    }

    public void Unregister(int id)
    {
        if (_tagsById.Remove(id))
        {
            _loop.UnregisterHotkey(id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _tagsById.Keys.ToList())
        {
            Unregister(id);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
        _loop.HotkeyPressed -= OnHotkeyPressed;
    }

    private void OnHotkeyPressed(object? sender, int id)
    {
        if (_tagsById.TryGetValue(id, out var tag))
        {
            HotkeyTriggered?.Invoke(this, new HotkeyTriggeredEventArgs(id, tag));
        }
    }
}
