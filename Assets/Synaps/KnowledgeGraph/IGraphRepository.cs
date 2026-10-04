using Synaps.Core;

namespace Synaps.KnowledgeGraph
{
    public sealed class GraphLoadResult
    {
        public readonly GraphDocument Document;
        public readonly bool RecoveredFromBackup;
        public GraphLoadResult(GraphDocument document, bool recoveredFromBackup = false)
        {
            Document = document;
            RecoveredFromBackup = recoveredFromBackup;
        }
    }

    public interface IGraphRepository
    {
        GraphLoadResult Load();
        void Save(GraphDocument document);
    }
}
