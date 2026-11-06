using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>
/// A list of rows that can be replaced in one step (P2.6-46). Everything watching a list works again on each change, so
/// adding rows one at a time did that work once per row: 9,730 list changes to read 138 mods.
/// </summary>
/// <typeparam name="T">The kind of row.</typeparam>
public sealed class RowList<T> : ObservableCollection<T>
{
    /// <summary>Puts <paramref name="rows"/> in the list's place and tells those watching once.</summary>
    public void ReplaceAll(IEnumerable<T> rows)
    {
        CheckReentrancy();
        Items.Clear();
        foreach (var row in rows)
        {
            Items.Add(row);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
