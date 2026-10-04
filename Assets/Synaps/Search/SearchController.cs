using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaps.Core;

namespace Synaps.Search
{
    public enum SearchStatus { Idle, Searching, Succeeded, Failed, Cancelled }

    public sealed class SearchState
    {
        public readonly string Query;
        public readonly SearchStatus Status;
        public readonly IReadOnlyList<SearchResult> Results;
        public readonly string Error;

        public SearchState(string query, SearchStatus status, IReadOnlyList<SearchResult> results, string error = null)
        {
            Query = query;
            Status = status;
            var copy = new List<SearchResult>();
            if (results != null) foreach (var result in results) copy.Add(result.Copy());
            Results = copy.AsReadOnly();
            Error = error;
        }
    }

    // Use on the owning synchronization context (Unity main thread). Await preserves that context.
    public sealed class SearchController : IDisposable
    {
        private readonly ISearchProvider provider;
        private CancellationTokenSource activeCancellation;
        private int requestVersion;
        private bool disposed;
        private SearchState state = new SearchState("", SearchStatus.Idle, null);
        public event Action<SearchState> StateChanged;
        public SearchState State { get { return new SearchState(state.Query, state.Status, state.Results, state.Error); } }

        public SearchController(ISearchProvider provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            this.provider = provider;
        }

        public async Task SearchAsync(string query, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (disposed) throw new ObjectDisposedException("SearchController");
            var version = ++requestVersion;
            if (activeCancellation != null) activeCancellation.Cancel();
            query = (query ?? "").Trim();
            if (query.Length == 0) { Publish(new SearchState("", SearchStatus.Idle, null)); return; }

            var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            activeCancellation = requestCancellation;
            try
            {
                Publish(new SearchState(query, SearchStatus.Searching, null));
                var results = await provider.SearchAsync(query, requestCancellation.Token);
                requestCancellation.Token.ThrowIfCancellationRequested();
                if (IsCurrent(version)) Publish(new SearchState(query, SearchStatus.Succeeded, results));
            }
            catch (OperationCanceledException)
            {
                if (IsCurrent(version)) Publish(new SearchState(query, SearchStatus.Cancelled, null));
            }
            catch (Exception exception)
            {
                if (IsCurrent(version)) Publish(new SearchState(query, SearchStatus.Failed, null, exception.Message));
            }
            finally
            {
                if (ReferenceEquals(activeCancellation, requestCancellation)) activeCancellation = null;
                requestCancellation.Dispose();
            }
        }

        private bool IsCurrent(int version) { return !disposed && requestVersion == version; }
        private void Publish(SearchState value)
        {
            state = value;
            var handler = StateChanged;
            if (handler != null) handler(State);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ++requestVersion;
            if (activeCancellation != null) activeCancellation.Cancel();
            StateChanged = null;
        }
    }
}
