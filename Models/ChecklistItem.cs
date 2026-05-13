using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopDo.Models;

public sealed class ChecklistItem : INotifyPropertyChanged
{
    private bool _isChecked;
    private string _text = string.Empty;

    public ChecklistItem(string text, bool isChecked = false)
    {
        _text = text;
        _isChecked = isChecked;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            OnPropertyChanged();
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
