using System;
using Synaps.Core;

namespace Synaps.KnowledgeGraph
{
    public sealed class WorkspaceController : IWorkspaceInteraction
    {
        private readonly KnowledgeGraphService graph;
        private string connectionOrigin;
        private string connectionRelation;
        public string SelectedNodeId { get; private set; }
        public bool IsConnecting { get { return connectionOrigin != null; } }
        public event Action SelectionChanged;

        public WorkspaceController(KnowledgeGraphService graph)
        {
            if (graph == null) throw new ArgumentNullException("graph");
            this.graph = graph;
        }

        public void SelectNode(string nodeId)
        {
            if (graph.FindNode(nodeId) == null) throw new ArgumentException("Cannot select a missing node.");
            if (connectionOrigin != null)
            {
                graph.Connect(connectionOrigin, nodeId, connectionRelation);
                connectionOrigin = null;
            }
            SelectedNodeId = nodeId;
            Notify();
        }

        public void MoveNode(string nodeId, SpatialPosition position) { graph.MoveNode(nodeId, position); }

        public void BeginConnection(string relationType)
        {
            if (SelectedNodeId == null || graph.FindNode(SelectedNodeId) == null)
                throw new InvalidOperationException("Select a source node first.");
            if (string.IsNullOrWhiteSpace(relationType)) throw new ArgumentException("Enter a relation type.");
            connectionOrigin = SelectedNodeId;
            connectionRelation = relationType;
            Notify();
        }

        public void CancelConnection() { connectionOrigin = null; Notify(); }
        public void DeleteSelected()
        {
            if (SelectedNodeId == null) return;
            graph.RemoveNode(SelectedNodeId);
            SelectedNodeId = null;
            connectionOrigin = null;
            Notify();
        }
        private void Notify() { var handler = SelectionChanged; if (handler != null) handler(); }
    }
}
