using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaps.Core;

namespace Synaps.AI
{
    public interface IAIService
    {
        Task<string> SummarizeAsync(string content, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> ExtractConceptsAsync(string content, CancellationToken cancellationToken);
        Task<IReadOnlyList<SuggestedEdge>> SuggestConnectionsAsync(KnowledgeNode node,
            IReadOnlyList<KnowledgeNode> candidates, CancellationToken cancellationToken);
    }
}
