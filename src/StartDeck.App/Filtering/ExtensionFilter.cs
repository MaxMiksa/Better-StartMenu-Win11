using System.IO;
using StartDeck.Models;

namespace StartDeck.Filtering;

public sealed class ExtensionFilter : IAppFilter
{
    private readonly string _extension;

    public ExtensionFilter(string extension = ".lnk")
    {
        _extension = extension.StartsWith('.') ? extension : "." + extension;
    }

    public bool ShouldKeep(AppEntry entry)
    {
        return string.Equals(Path.GetExtension(entry.FullPath), _extension, StringComparison.OrdinalIgnoreCase);
    }
}
