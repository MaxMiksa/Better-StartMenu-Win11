using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace StartDeck.ViewModels;

public sealed class AppEntryViewModel : INotifyPropertyChanged
{
    private BitmapImage? _icon;

    public string Name { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;

    public string Group { get; init; } = string.Empty;

    public bool IsUserScope { get; init; }

    public BitmapImage? Icon
    {
        get => _icon;
        set
        {
            if (_icon != value)
            {
                _icon = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
