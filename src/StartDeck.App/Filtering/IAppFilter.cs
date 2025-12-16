using StartDeck.Models;

namespace StartDeck.Filtering;

public interface IAppFilter
{
    bool ShouldKeep(AppEntry entry);
}
