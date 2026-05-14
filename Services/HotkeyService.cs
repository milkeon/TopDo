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
    Up = 0x26,
    Down = 0x28,
    Q = 0x51,
    T = 0x54,
    Space = 0x20
}
