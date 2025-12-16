using System;
using StartDeck.Models;

namespace StartDeck.Filtering;

public sealed class KeywordFilter : IAppFilter
{
    private static readonly string[] Blocked =
    {
        "uninstall", "卸载", "help", "帮助", "readme", "说明", "website", "网站", "url"
    };

    public bool ShouldKeep(AppEntry entry)
    {
        var name = entry.Name.AsSpan();
        foreach (var blocked in Blocked)
        {
            if (name.IndexOf(blocked.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
        }

        return true;
    }
}
