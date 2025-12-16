using StartDeck.Models;

namespace StartDeck.Sources;

public interface IAppSource
{
    Task<IReadOnlyList<AppEntry>> GetAppsAsync(CancellationToken cancellationToken);
}
