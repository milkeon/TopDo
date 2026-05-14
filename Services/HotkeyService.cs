namespace TopDo.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public enum HotkeyKeys : uint
{
    Enter = 0x0D,
    Up = 0x26,
    Down = 0x28,
    PageUp = 0x21,
    PageDown = 0x22,
    Q = 0x51,
    T = 0x54,
    Space = 0x20
}
