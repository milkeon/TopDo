using System.Windows;
using System.Windows.Forms;
using TopDo.Services;

namespace TopDo;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();
        _mainWindow.ShowInTaskbar = false;

        _mainWindow.RequestShow += ShowMainWindow;
        _mainWindow.RequestExit += ExitApplication;
        _mainWindow.RegisterGlobalHotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKeys.Space);

        _mainWindow.Show();
        _mainWindow.Hide();

        _trayIcon = new NotifyIcon
        {
            Text = "Top Do",
            Visible = true,
            Icon = System.Drawing.SystemIcons.Application,
            ContextMenuStrip = BuildTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
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
