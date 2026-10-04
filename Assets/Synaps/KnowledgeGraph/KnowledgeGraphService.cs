using System;
using System.Collections.Generic;
using Synaps.Core;

namespace Synaps.KnowledgeGraph
{
    // Single owner on the main thread. All public reads return detached copies.
    public sealed class KnowledgeGraphService
    {
        private readonly IGraphRepository repository;
        private GraphDocument document = new GraphDocument();
        public event Action Changed;
        public bool IsDirty { get; private set; }

        public KnowledgeGraphService(IGraphRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            this.repository = repository;
        }

        public GraphLoadResult Load()
        {
            var loaded = repository.Load();
            GraphValidator.Validate(loaded.Document);
            document = loaded.Document.Copy();
            IsDirty = loaded.RecoveredFromBackup;
            Notify();
            return new GraphLoadResult(document.Copy(), loaded.RecoveredFromBackup);
        }

        public void Save()
        {
            repository.Save(document.Copy()); // Failure leaves IsDirty true for retry.
            IsDirty = false;
        }

        public GraphDocument Snapshot() { return document.Copy(); }

        public KnowledgeNode FindNode(string id)
        {
            var node = document.Nodes.Find(item => item.Id == id);
            return node == null ? null : node.Copy();
        }

        public KnowledgeNode SaveSearchResult(SearchResult result, SpatialPosition position)
        {
            if (result == null) throw new ArgumentNullException("result");
            if (string.IsNullOrWhiteSpace(result.Id) || string.IsNullOrWhiteSpace(result.Title) ||
                !Enum.IsDefined(typeof(SearchResultType), result.Type))
                throw new ArgumentException("A search result needs a stable ID, title, and valid type.", "result");
            RequirePosition(position);
            var existing = document.Nodes.Find(item => item.SearchResultId == result.Id);
            if (existing != null) return existing.Copy();
            var now = DateTime.UtcNow.Ticks;
            var nodeType = result.Type == SearchResultType.Paper ? KnowledgeNodeType.Paper :
                result.Type == SearchResultType.Video ? KnowledgeNodeType.Video :
                result.Type == SearchResultType.Note ? KnowledgeNodeType.Note : KnowledgeNodeType.WebPage;
            var node = new KnowledgeNode
            {
                Id = Guid.NewGuid().ToString("N"), SearchResultId = result.Id,
                Title = result.Title.Trim(), Content = result.Snippet ?? "", Summary = "",
                SourceUrl = result.Url ?? "", NodeType = nodeType, Position = position,
                CreatedAtUtcTicks = now, UpdatedAtUtcTicks = now
            };
            document.Nodes.Add(node);
            Commit();
            return node.Copy();
        }

        public void MoveNode(string id, SpatialPosition position)
        {
            RequirePosition(position);
            var node = RequireNode(id);
            if (node.Position.X == position.X && node.Position.Y == position.Y && node.Position.Z == position.Z) return;
            node.Position = position;
            node.UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;
            Commit();
        }

        public void UpdateSummary(string id, string summary)
        {
            var node = RequireNode(id);
            node.Summary = summary ?? "";
            node.UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;
            Commit();
        }

        public KnowledgeEdge Connect(string fromId, string toId, string relationType)
        {
            RequireNode(fromId);
            RequireNode(toId);
            if (fromId == toId) throw new ArgumentException("Select a different target node.");
            if (string.IsNullOrWhiteSpace(relationType)) throw new ArgumentException("A relation type is required.");
            var relation = relationType.Trim().ToLowerInvariant();
            var existing = document.Edges.Find(item => item.FromNodeId == fromId && item.ToNodeId == toId &&
                string.Equals(item.RelationType, relation, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing.Copy();
            var edge = new KnowledgeEdge { Id = Guid.NewGuid().ToString("N"), FromNodeId = fromId,
                ToNodeId = toId, RelationType = relation };
            document.Edges.Add(edge);
            Commit();
            return edge.Copy();
        }

        public void RemoveNode(string id)
        {
            var node = RequireNode(id);
            document.Nodes.Remove(node);
            document.Edges.RemoveAll(edge => edge.FromNodeId == id || edge.ToNodeId == id);
            Commit();
        }

        public void RemoveEdge(string id)
        {
            if (document.Edges.RemoveAll(edge => edge.Id == id) > 0) Commit();
        }

        public IReadOnlyList<KnowledgeNode> GetNeighbours(string id)
        {
            RequireNode(id);
            var ids = new HashSet<string>();
            foreach (var edge in document.Edges)
            {
                if (edge.FromNodeId == id) ids.Add(edge.ToNodeId);
                if (edge.ToNodeId == id) ids.Add(edge.FromNodeId);
            }
            return document.Nodes.FindAll(node => ids.Contains(node.Id)).ConvertAll(node => node.Copy()).AsReadOnly();
        }

        private KnowledgeNode RequireNode(string id)
        {
            var node = document.Nodes.Find(item => item.Id == id);
            if (node == null) throw new KeyNotFoundException("Node does not exist: " + id);
            return node;
        }
        private static void RequirePosition(SpatialPosition position)
        {
            if (!position.IsFinite) throw new ArgumentException("Position must be finite.");
        }
        private void Commit() { IsDirty = true; Notify(); }
        private void Notify() { var handler = Changed; if (handler != null) handler(); }
    }
}
