using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TopDo.Models;
using TopDo.Services;

namespace TopDo;

public partial class MainWindow : Window
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x1200;
    private const int OpacityUpHotkeyId = 0x1201;
    private const int OpacityDownHotkeyId = 0x1202;
    internal const int ExitHotkeyId = 0x1203;
    private const double BackdropStep = 0.18;
    private const double DefaultBackdropOpacity = 0.56;
    private const double MinBackdropOpacity = 0.30;
    private const double MaxBackdropOpacity = 0.90;
    private HwndSource? _source;
    private readonly ObservableCollection<ToDoItem> _items = new();
    private readonly ObservableCollection<ChecklistDraftItem> _composerChecklistItems = new();
    private bool _restoringWindowState;
    private bool _windowStateReady;
    private bool _composerExpanded;

    public event Action? RequestShow;
    public event Action? RequestExit;

    public ObservableCollection<ToDoItem> Items => _items;

    public ObservableCollection<ChecklistDraftItem> ComposerChecklistItems => _composerChecklistItems;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += Window_Loaded;
        LocationChanged += Window_StateChanged;
        SizeChanged += Window_StateChanged;
        StateChanged += Window_StateChanged;
    }

    public void RegisterGlobalHotkey(HotkeyModifiers modifiers, HotkeyKeys key, int? hotkeyId = null)
    {
        SourceInitialized += (_, _) =>
        {
            _source = (HwndSource)PresentationSource.FromVisual(this)!;
            _source.AddHook(WndProc);
            var helper = new WindowInteropHelper(this);
            NativeMethods.RegisterHotKey(helper.Handle, hotkeyId ?? HotkeyId, (uint)modifiers, (uint)key);
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSeedData();
        RestoreWindowState();
        SetComposerDefaults();
        SetComposerExpanded(false, focusTitle: true);
        BackdropLayer.Opacity = DefaultBackdropOpacity;
        Dispatcher.BeginInvoke(new Action(SetComposerDefaults));
    }

    private void LoadSeedData()
    {
        if (_items.Count > 0) return;

        _items.Add(new ToDoItem(
            "예시 투두",
            string.Empty,
            DateTime.Now.AddMinutes(-15),
            DateTime.Today.AddHours(9),
            new[]
            {
                new ChecklistItem("유리형 입력창 예시 확인", true),
                new ChecklistItem("숨겨진 내용 hover 확인", false),
                new ChecklistItem("상위 체크박스 자동 동기화", false),
            }));

        _items.Add(new ToDoItem(
            "오늘 회의 정리",
            string.Empty,
            DateTime.Now.AddMinutes(-40),
            DateTime.Today.AddHours(13).AddMinutes(30),
            new[]
            {
                new ChecklistItem("회의 메모 수집", true),
                new ChecklistItem("핵심 의사결정 추리기", true),
                new ChecklistItem("후속 작업 표시", false),
            }));

        _items.Add(new ToDoItem(
            "문서 초안 확인",
            string.Empty,
            DateTime.Now.AddMinutes(-75),
            DateTime.Today.AddHours(18),
            new[]
            {
                new ChecklistItem("표현 통일", true),
                new ChecklistItem("날짜/시간 확인", false),
            }));
    }

    private void RestoreWindowState()
    {
        _windowStateReady = true;

        var state = WindowStateStore.Load();
        if (state is null) return;

        _restoringWindowState = true;
        Left = state.Left;
        Top = state.Top;
        Width = state.Width;
        Height = state.Height;
        WindowState = state.WindowState;
        _restoringWindowState = false;
    }

    private void SaveWindowState()
    {
        if (_restoringWindowState) return;
        if (!_windowStateReady) return;
        if (WindowState != WindowState.Normal) return;

        WindowStateStore.Save(new WindowGeometryState(Left, Top, Width, Height, WindowState.Normal));
    }

    private void EnsureComposerDraftRow()
    {
        if (_composerChecklistItems.Count == 0)
        {
            _composerChecklistItems.Add(new ChecklistDraftItem());
        }
    }

    private void SetComposerExpanded(bool expanded, bool focusTitle = false)
    {
        ToggleComposerDetails(expanded, focusDetail: false);

        if (!expanded && focusTitle)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TitleBox.Focus();
                TitleBox.CaretIndex = TitleBox.Text.Length;
            }));
        }
    }

    private void ToggleComposerDetails(bool expanded, bool focusDetail = false)
    {
        _composerExpanded = expanded;
        ComposerDetailsPanel.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        ComposerActionButton.Content = expanded ? "완료" : "추가";

        if (expanded)
        {
            EnsureComposerDraftRow();
            if (focusDetail)
            {
                FocusChecklistItem(_composerChecklistItems.Count - 1);
            }
            return;
        }

        if (focusDetail)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TitleBox.Focus();
                TitleBox.CaretIndex = TitleBox.Text.Length;
            }));
        }
    }

    private void TitleBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_composerExpanded)
        {
            SetComposerExpanded(true);
        }
    }

    private void TitleBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_composerExpanded)
        {
            SetComposerExpanded(true);
        }
    }

    private void ExpandComposerForChecklist()
    {
        if (!_composerExpanded)
        {
            ToggleComposerDetails(true, focusDetail: true);
            return;
        }

        EnsureComposerDraftRow();
        FocusChecklistItem(_composerChecklistItems.Count - 1);
    }

    private void CompleteComposer()
    {
        var title = TitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var checklist = _composerChecklistItems
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => new ChecklistItem(item.Text.Trim(), item.IsChecked))
            .ToList();
        var content = BuildChecklistContent(checklist);

        var dueDate = ParseDueDate(DueDateBox.Text);
        var dueTimeText = string.IsNullOrWhiteSpace(DueTimeBox.Text) ? DateTime.Now.AddHours(1).ToString("HH:mm") : DueTimeBox.Text.Trim();
        if (!TimeSpan.TryParse(dueTimeText, out var time))
        {
            time = DateTime.Now.AddHours(1).TimeOfDay;
        }

        var dueAt = dueDate.Date.Add(time);
        _items.Insert(0, new ToDoItem(title, content, DateTime.Now, dueAt, checklist));

        ResetComposer();
        SetComposerExpanded(false, focusTitle: true);
    }

    private void ResetComposer()
    {
        TitleBox.Clear();
        SetComposerDefaults();
        _composerChecklistItems.Clear();
        _composerChecklistItems.Add(new ChecklistDraftItem());
    }

    private void ComposerActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_composerExpanded)
        {
            CompleteComposer();
            return;
        }

        ToggleComposerDetails(true, focusDetail: true);
    }

    private void RemoveChecklistItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ChecklistDraftItem item })
        {
            return;
        }

        var index = _composerChecklistItems.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        _composerChecklistItems.RemoveAt(index);
        EnsureComposerDraftRow();
        FocusChecklistItem(Math.Max(0, Math.Min(index, _composerChecklistItems.Count - 1)));
    }

    private void ChecklistTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox || textBox.DataContext is not ChecklistDraftItem item)
        {
            return;
        }

        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            CompleteComposer();
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            var index = _composerChecklistItems.IndexOf(item);
            if (index < 0)
            {
                return;
            }

            _composerChecklistItems.Insert(index + 1, new ChecklistDraftItem());
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChecklistItemsControl.UpdateLayout();
                FocusChecklistItem(index + 1);
            }));
        }
    }

    private void TitleBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            if (_composerExpanded)
            {
                CompleteComposer();
            }
            else
            {
                ExpandComposerForChecklist();
            }
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ToDoItem item })
        {
            _items.Remove(item);
        }
    }

    private void WindowRoot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (IsInteractiveSource(source) || IsWithinResizeBorder(e.GetPosition(this)))
        {
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // ignore when drag is not possible
            }
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetComposerDefaults()
    {
        DueDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        DueTimeBox.Text = DateTime.Now.AddHours(1).ToString("HH:mm");
    }

    private void DueDateBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        }
    }

    private void DueTimeBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = DateTime.Now.AddHours(1).ToString("HH:mm");
        }
    }

    private static DateTime ParseDueDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DateTime.Today;
        }

        var trimmed = text.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var exact))
        {
            return exact.Date;
        }

        if (DateTime.TryParse(trimmed, out var parsed))
        {
            return parsed.Date;
        }

        return DateTime.Today;
    }

    private void FocusChecklistItem(int index)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ChecklistItemsControl.UpdateLayout();
            if (ChecklistItemsControl.ItemContainerGenerator.ContainerFromIndex(index) is not DependencyObject container)
            {
                return;
            }

            var textBox = FindVisualChild<System.Windows.Controls.TextBox>(container);
            if (textBox is null)
            {
                return;
            }

            textBox.Focus();
            textBox.CaretIndex = textBox.Text.Length;
        }));
    }

    private static string BuildChecklistContent(IEnumerable<ChecklistItem> checklist)
    {
        var lines = checklist
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => $"{(item.IsChecked ? "☑" : "☐")} {item.Text.Trim()}")
            .ToList();

        return string.Join(Environment.NewLine, lines);
    }

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        SaveWindowState();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private static bool IsInteractiveSource(DependencyObject source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is System.Windows.Controls.TextBox
                || current is System.Windows.Controls.Button
                || current is System.Windows.Controls.CheckBox
                || current is DatePicker
                || current is System.Windows.Controls.ComboBox
                || current is System.Windows.Controls.Primitives.RangeBase
                || current is System.Windows.Controls.Primitives.ButtonBase)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsWithinResizeBorder(System.Windows.Point position)
    {
        const double resizeMargin = 14;
        return position.X <= resizeMargin
               || position.Y <= resizeMargin
               || position.X >= ActualWidth - resizeMargin
               || position.Y >= ActualHeight - resizeMargin;
    }

    private void AdjustBackdropOpacity(double delta)
    {
        var nextOpacity = Math.Clamp(BackdropLayer.Opacity + delta, MinBackdropOpacity, MaxBackdropOpacity);
        BackdropLayer.Opacity = nextOpacity;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
        {
            if (wParam.ToInt32() == HotkeyId)
            {
                RequestShow?.Invoke();
                handled = true;
            }
            else if (wParam.ToInt32() == OpacityUpHotkeyId)
            {
                AdjustBackdropOpacity(BackdropStep);
                handled = true;
            }
            else if (wParam.ToInt32() == OpacityDownHotkeyId)
            {
                AdjustBackdropOpacity(-BackdropStep);
                handled = true;
            }
            else if (wParam.ToInt32() == ExitHotkeyId)
            {
                RequestExit?.Invoke();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }
}

public sealed class ChecklistDraftItem
{
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
}
