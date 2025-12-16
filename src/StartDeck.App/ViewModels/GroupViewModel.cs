using System.Collections.ObjectModel;

namespace StartDeck.ViewModels;

public sealed class GroupViewModel
{
    public string Name { get; init; } = string.Empty;

    public ObservableCollection<AppEntryViewModel> Items { get; } = new();
}
