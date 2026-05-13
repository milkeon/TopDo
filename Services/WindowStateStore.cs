using System.IO;
using System.Text.Json;
using System.Windows;

namespace TopDo.Services;

public sealed record WindowGeometryState(double Left, double Top, double Width, double Height, WindowState WindowState);

public static class WindowStateStore
{
    private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TopDo");
    private static readonly string FilePath = Path.Combine(FolderPath, "window-state.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static WindowGeometryState? Load()
    {
        if (!File.Exists(FilePath)) return null;

        try
        {
            var json = File.ReadAllText(FilePath);
            var state = JsonSerializer.Deserialize<WindowGeometryState>(json, JsonOptions);
            if (state is null) return null;
            if (state.Width < 300 || state.Height < 400) return null;
            return state;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(WindowGeometryState state)
    {
        Directory.CreateDirectory(FolderPath);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
