using System.Windows;
using Forms = System.Windows.Forms;
using TopDo.Services;

namespace TopDo;

public partial class App : System.Windows.Application
{
    private MainWindow? _mainWindow;
    private Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();
        _mainWindow.ShowInTaskbar = true;

        _mainWindow.RequestShow += ShowMainWindow;
        _mainWindow.RequestExit += ExitApplication;
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.Space, 0x1200);
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.Up, 0x1201);
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.Down, 0x1202);
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.PageUp, 0x1204);
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.PageDown, 0x1205);
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.Q, 0x1203);

        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Top Do",
            Visible = true,
            Icon = System.Drawing.SystemIcons.Application,
            ContextMenuStrip = BuildTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("열기", null, (_, _) => ShowMainWindow());
        menu.Items.Add("종료", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();
    }

    private void ExitApplication()
    {
        _trayIcon?.Dispose();
        Shutdown();
    }
}
