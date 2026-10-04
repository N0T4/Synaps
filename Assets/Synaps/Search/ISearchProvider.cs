using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaps.Core;

namespace Synaps.Search
{
    public interface ISearchProvider
    {
        Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken);
    }
}
