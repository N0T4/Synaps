using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaps.Core;

namespace Synaps.AI
{
    // Deterministic offline placeholder. No model, credentials, or speculative graph connections.
    public sealed class MockAIService : IAIService
    {
        public Task<string> SummarizeAsync(string content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = (content ?? "").Trim();
            return Task.FromResult("[Mock summary] " + (text.Length > 160 ? text.Substring(0, 160) + "..." : text));
        }

        public Task<IReadOnlyList<string>> ExtractConceptsAsync(string content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<string> result = new List<string>().AsReadOnly();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<SuggestedEdge>> SuggestConnectionsAsync(KnowledgeNode node,
            IReadOnlyList<KnowledgeNode> candidates, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node == null) throw new ArgumentNullException("node");
            IReadOnlyList<SuggestedEdge> result = new List<SuggestedEdge>().AsReadOnly();
            return Task.FromResult(result);
        }
    }
}
