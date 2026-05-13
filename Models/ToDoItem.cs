using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopDo.Models;

public sealed class ToDoItem : INotifyPropertyChanged
{
    private readonly ObservableCollection<ChecklistItem> _checklist = new();
    private readonly HashSet<ChecklistItem> _observedChecklistItems = new();
    private bool _isDone;
    private bool _syncingChecklistState;
    private string _content = string.Empty;

    public ToDoItem(string title, string content, DateTime createdAt, DateTime dueAt, IEnumerable<ChecklistItem>? checklist = null)
    {
        Title = title;
        _content = content;
        CreatedAt = createdAt;
        DueAt = dueAt;

        _checklist.CollectionChanged += Checklist_CollectionChanged;

        if (checklist is not null)
        {
            foreach (var item in checklist)
            {
                _checklist.Add(item);
            }
        }

        RefreshDerivedState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; set; }

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value)
            {
                return;
            }

            _content = value;
            OnPropertyChanged();
        }
    }

    public bool IsDone
    {
        get => _isDone;
        set => SetIsDone(value, syncChildren: true);
    }

    public DateTime CreatedAt { get; }

    public DateTime DueAt { get; }

    public ObservableCollection<ChecklistItem> Checklist => _checklist;

    private void Checklist_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ChecklistItem item in e.NewItems)
            {
                if (_observedChecklistItems.Add(item))
                {
                    item.PropertyChanged += ChecklistItem_PropertyChanged;
                }
            }
        }

        if (e.OldItems is not null)
        {
            foreach (ChecklistItem item in e.OldItems)
            {
                if (_observedChecklistItems.Remove(item))
                {
                    item.PropertyChanged -= ChecklistItem_PropertyChanged;
                }
            }
        }

        RefreshDerivedState();
    }

    private void ChecklistItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncingChecklistState)
        {
            return;
        }

        if (e.PropertyName == nameof(ChecklistItem.IsChecked) || e.PropertyName == nameof(ChecklistItem.Text))
        {
            RefreshDerivedState();
        }
    }

    private void RefreshDerivedState()
    {
        if (_checklist.Count == 0)
        {
            Content = string.Empty;
            SetIsDone(false, syncChildren: false);
            return;
        }

        Content = string.Join(Environment.NewLine, _checklist.Select(item => $"{(item.IsChecked ? "☑" : "☐")} {item.Text}".TrimEnd()));
        SetIsDone(_checklist.All(item => item.IsChecked), syncChildren: false);
    }

    private void SetIsDone(bool value, bool syncChildren)
    {
        if (_isDone == value)
        {
            if (syncChildren && _checklist.Count > 0)
            {
                _syncingChecklistState = true;
                foreach (var item in _checklist)
                {
                    item.IsChecked = value;
                }
                _syncingChecklistState = false;
                Content = string.Join(Environment.NewLine, _checklist.Select(item => $"{(item.IsChecked ? "☑" : "☐")} {item.Text}".TrimEnd()));
            }

            return;
        }

        _isDone = value;
        OnPropertyChanged(nameof(IsDone));

        if (syncChildren && _checklist.Count > 0)
        {
            _syncingChecklistState = true;
            foreach (var item in _checklist)
            {
                item.IsChecked = value;
            }
            _syncingChecklistState = false;
            Content = string.Join(Environment.NewLine, _checklist.Select(item => $"{(item.IsChecked ? "☑" : "☐")} {item.Text}".TrimEnd()));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
