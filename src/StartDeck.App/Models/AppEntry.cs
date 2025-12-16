namespace StartDeck.Models;

public sealed class AppEntry
{
    public string Name { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string? TargetPath { get; init; }

    public string Group { get; init; } = string.Empty;

    public bool IsUserScope { get; init; }

    public bool IsValid { get; init; }
}
