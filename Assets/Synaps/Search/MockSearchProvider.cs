using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaps.Core;

namespace Synaps.Search
{
    // Fixtures, not paper metadata. Deliberately no invented citations or live network calls.
    public sealed class MockSearchProvider : ISearchProvider
    {
        private readonly int latencyMilliseconds;
        private static readonly string[] Titles = { "FingerType", "BiTipText", "H4VR", "TapGazer", "PinchType" };

        public MockSearchProvider(int latencyMilliseconds = 250)
        {
            if (latencyMilliseconds < 0) throw new ArgumentOutOfRangeException("latencyMilliseconds");
            this.latencyMilliseconds = latencyMilliseconds;
        }

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<SearchResult>().AsReadOnly();
            if (latencyMilliseconds > 0) await Task.Delay(latencyMilliseconds, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var results = new List<SearchResult>();
            for (var i = 0; i < Titles.Length; i++)
                results.Add(new SearchResult
                {
                    Id = "mock-" + Titles[i].ToLowerInvariant(),
                    Title = Titles[i],
                    Url = "", // Replace only with verified source URLs in the real provider.
                    Snippet = "Demo research card for \"" + query.Trim() + "\". Replace with verified source content.",
                    Source = "Mock data",
                    Type = SearchResultType.Paper
                });
            return results.AsReadOnly();
        }
    }
}
